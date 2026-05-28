using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Cells.Handlers.Commands;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Cell;
using Npgsql;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Cells.Handlers;

public class UpdateCellTechnologiesBandHandlerTests
{
    private static readonly Guid CellId = Guid.NewGuid();
    private static readonly Guid SectorId = Guid.NewGuid();
    private static readonly Guid ServiceProviderId = Guid.NewGuid();
    private static readonly Guid TechnologyId = Guid.NewGuid();
    private static readonly Guid BandId = Guid.NewGuid();
    private static readonly Guid NetworkId = Guid.NewGuid();
    private static readonly Guid SystemTypeId = Guid.NewGuid();
    private static readonly Guid MunicipalityId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WhenCellNotFound_ReturnsError()
    {
        var handler = CreateHandler(cells: []);

        var result = await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("NullCell");
    }

    [Fact]
    public async Task Handle_WhenNoSectors_ReturnsError()
    {
        var handler = CreateHandler(cells: [MakeCell()], sectors: []);

        var result = await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("NoSectors");
    }

    [Fact]
    public async Task Handle_WhenInvalidTechnologyId_ReturnsError()
    {
        var handler = CreateHandler(technologies: []);

        var result = await handler.Handle(
            MakeRequest([new CellTechnologyBandInputDto { TechnologyId = Guid.NewGuid(), BandId = BandId }]),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("InvalidTechnologies");
    }

    [Fact]
    public async Task Handle_WhenInvalidBandId_ReturnsError()
    {
        var handler = CreateHandler(bands: []);

        var result = await handler.Handle(
            MakeRequest([new CellTechnologyBandInputDto { TechnologyId = TechnologyId, BandId = Guid.NewGuid() }]),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("InvalidBands");
    }

    [Fact]
    public async Task Handle_WhenCellDefaultServiceProviderInvalid_ReturnsError()
    {
        var handler = CreateHandler(serviceProviders: []); // cell's default provider absent

        var result = await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("InvalidServiceProviders");
    }

    [Fact]
    public async Task Handle_WhenOverrideServiceProviderIdInvalid_ReturnsError()
    {
        // Only default provider exists; override with unknown ID
        var handler = CreateHandler(serviceProviders: [MakeServiceProvider()]);

        var result = await handler.Handle(
            MakeRequest([new CellTechnologyBandInputDto
            {
                TechnologyId = TechnologyId,
                BandId = BandId,
                ServiceProviderId = Guid.NewGuid() // unknown
            }]),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("InvalidServiceProviders");
    }

    [Fact]
    public async Task Handle_WhenSystemTypeNotFound_ReturnsNetworkError()
    {
        var handler = CreateHandler(systemTypes: []); // no SystemType => network cannot be found/created

        var result = await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Network");
    }

    [Fact]
    public async Task Handle_WhenEmptyPayload_DeletesAllExistingCellulars()
    {
        var existing = MakeCellular();
        var deleted = new List<Cellular>();
        var handler = CreateHandler(cellulars: [existing], onDelete: deleted.Add);

        var result = await handler.Handle(MakeRequest([]), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        deleted.Should().ContainSingle(c => c.Id == existing.Id);
    }

    [Fact]
    public async Task Handle_WhenNewCombinationRequested_CreatesExpectedCellularRecord()
    {
        var added = new List<Cellular>();
        var handler = CreateHandler(onAdd: added.Add);

        var result = await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        added.Should().ContainSingle(c =>
            c.CellSectorId == SectorId &&
            c.TechnologyId == TechnologyId &&
            c.BandId == BandId &&
            c.ServiceProviderId == ServiceProviderId);
    }

    [Fact]
    public async Task Handle_WhenNewCellularCreated_PopulatesRequiredFields()
    {
        var added = new List<Cellular>();
        var handler = CreateHandler(onAdd: added.Add);

        await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        added.Should().ContainSingle();
        var c = added[0];
        c.NetworkId.Should().NotBe(Guid.Empty);
        c.DLCenterFreq.Should().BeGreaterThan(0);
        c.ULCenterFreq.Should().BeGreaterThan(0);
        c.Bandwidth.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WhenExistingCombinationNotInPayload_DeletesObsoleteCellular()
    {
        var obsoleteTechId = Guid.NewGuid();
        var keptTechId = Guid.NewGuid();

        var obsolete = MakeCellular(techId: obsoleteTechId);
        var kept = MakeCellular(techId: keptTechId);
        var deleted = new List<Cellular>();

        var handler = CreateHandler(
            cellulars: [obsolete, kept],
            technologies: [new Technology { Id = keptTechId, Standard = "NR" }],
            systemTypes: [new SystemType { Id = SystemTypeId, TechnologyId = keptTechId, Name = "NR" }],
            onDelete: deleted.Add);

        var result = await handler.Handle(
            MakeRequest([new CellTechnologyBandInputDto { TechnologyId = keptTechId, BandId = BandId }]),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        deleted.Should().ContainSingle(c => c.Id == obsolete.Id);
    }

    [Fact]
    public async Task Handle_WhenDuplicateExistingCombinationPresent_DeletesDuplicateRows()
    {
        var duplicateA = MakeCellular();
        var duplicateB = MakeCellular();
        duplicateB.CreatedAt = DateTime.UtcNow; // newer than duplicateA (default DateTime.MinValue) → duplicate to delete
        var deleted = new List<Cellular>();
        var handler = CreateHandler(
            cellulars: [duplicateA, duplicateB],
            onDelete: deleted.Add);

        var result = await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        deleted.Should().ContainSingle(c => c.Id == duplicateB.Id);
    }

    [Fact]
    public async Task Handle_WhenSectorHasNoCellulars_CreatesMissingSectorRow()
    {
        var addedSectors = new List<Sector>();
        var handler = CreateHandler(onAddSector: addedSectors.Add);

        await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        addedSectors.Should().ContainSingle(s => s.CellSectorId == SectorId);
    }

    [Fact]
    public async Task Handle_WhenSectorAlreadyExists_DoesNotCreateDuplicateSector()
    {
        var existingSectors = new List<Sector>
        {
            new() { Id = Guid.NewGuid(), CellSectorId = SectorId }
        };
        var addedSectors = new List<Sector>();
        var handler = CreateHandler(existingSectors: existingSectors, onAddSector: addedSectors.Add);

        await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        addedSectors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMultipleSectorsMissing_BatchesSectorCreationIntoSingleSave()
    {
        var secondSectorId = Guid.NewGuid();
        var sectors = new List<CellSector>
        {
            MakeCellSector(),
            new()
            {
                Id = secondSectorId,
                CellId = CellId,
                AntennaType = AntennaTypeEnum.Unknown,
                AntennaHeight = 30m,
                Azimuth = 0m,
                BeamTilt = 0m
            }
        };

        var saveCalls = 0;
        var handler = CreateHandler(
            sectors: sectors,
            onSectorSaveChanges: () => saveCalls++);

        await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        saveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenNetworkAlreadyExists_ReusesExistingNetwork()
    {
        var existingNetwork = new Network
        {
            Id = NetworkId,
            Name = "LTE_Band 3_TestMunicipality",
            ServiceProviderId = ServiceProviderId,
            SystemTypeId = SystemTypeId,
            BandId = BandId,
            DLCenterFreq = 1842.5m,
            ULCenterFreq = 1747.5m,
            Bandwidth = 20m
        };

        var added = new List<Cellular>();
        var handler = CreateHandler(networks: [existingNetwork], onAdd: added.Add);

        await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        added.Should().ContainSingle(c => c.NetworkId == existingNetwork.Id);
    }

    [Fact]
    public async Task Handle_WhenConcurrentInsertCausesUniqueViolation_ReturnsSuccess()
    {
        var handler = CreateHandler(simulateConcurrentUniqueViolationOnCellularSave: true);

        var result = await handler.Handle(MakeRequest([MakeItem()]), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().ContainSingle(x =>
            x.TechnologyId == TechnologyId &&
            x.Bands.Any(b => b.BandId == BandId));
    }

    // ── Request/entity builders ───────────────────────────────────────────────

    private static UpdateCellTechnologiesBandRequest MakeRequest(
        List<CellTechnologyBandInputDto> items) =>
        new(CellId, new UpdateCellTechnologiesBandDto { TechnologyBands = items });

    private static CellTechnologyBandInputDto MakeItem(Guid? techId = null, Guid? bandId = null) =>
        new() { TechnologyId = techId ?? TechnologyId, BandId = bandId ?? BandId };

    private static Cell MakeCell() => new()
    {
        Id = CellId,
        ServiceProviderId = ServiceProviderId,
        Location = GeometryHelper.CreatePoint(new Coordinate(121.0, 14.0)),
        Address = "Test Address",
        Name = "TestCell",
        CID = "1",
        TAC = "2",
        SiteId = "SITE-1"
    };

    private static CellSector MakeCellSector() => new()
    {
        Id = SectorId,
        CellId = CellId,
        AntennaType = AntennaTypeEnum.Unknown,
        AntennaHeight = 30m,
        Azimuth = 0m,
        BeamTilt = 0m
    };

    private static Cellular MakeCellular(
        Guid? sectorId = null, Guid? techId = null, Guid? bandId = null,
        Guid? providerId = null) => new()
        {
            Id = Guid.NewGuid(),
            CellSectorId = sectorId ?? SectorId,
            TechnologyId = techId ?? TechnologyId,
            BandId = bandId ?? BandId,
            ServiceProviderId = providerId ?? ServiceProviderId,
            NetworkId = NetworkId,
            DLCenterFreq = 1842.5m,
            ULCenterFreq = 1747.5m,
            Bandwidth = 20m,
            CellSector = new CellSector { Id = sectorId ?? SectorId, CellId = CellId }
        };

    private static Technology MakeTechnology() =>
        new() { Id = TechnologyId, Standard = "LTE" };

    private static Band MakeBand() =>
        new() { Id = BandId, Name = "Band 3" };

    private static ServiceProvider MakeServiceProvider() => new()
    {
        Id = ServiceProviderId,
        Provider = new Provider { Id = Guid.NewGuid(), Name = "Globe", Color = "#FFF" },
        Service = new Service { Id = Guid.NewGuid(), Name = "Wireless", Type = ServiceTypeEnum.Wireless }
    };

    private static SystemType MakeSystemType(Guid? techId = null) =>
        new() { Id = SystemTypeId, TechnologyId = techId ?? TechnologyId, Name = "LTE" };

    // ── Handler factory ───────────────────────────────────────────────────────

    private static UpdateCellTechnologiesBandHandler CreateHandler(
        List<Cell>? cells = null,
        List<CellSector>? sectors = null,
        List<Cellular>? cellulars = null,
        List<Technology>? technologies = null,
        List<Band>? bands = null,
        List<ServiceProvider>? serviceProviders = null,
        List<SystemType>? systemTypes = null,
        List<Network>? networks = null,
        List<Sector>? existingSectors = null,
        Action<Cellular>? onAdd = null,
        Action<Cellular>? onDelete = null,
        Action<Sector>? onAddSector = null,
        Action? onSectorSaveChanges = null,
        bool simulateConcurrentUniqueViolationOnCellularSave = false)
    {
        cells ??= [MakeCell()];
        sectors ??= [MakeCellSector()];
        cellulars ??= [];
        technologies ??= [MakeTechnology()];
        bands ??= [MakeBand()];
        serviceProviders ??= [MakeServiceProvider()];
        systemTypes ??= [MakeSystemType()];
        networks ??= [];
        existingSectors ??= [];

        // Cell repository
        var cellRepoMock = new Mock<ICellRepository>();
        cellRepoMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Cell, bool>>?>()))
            .Returns((System.Linq.Expressions.Expression<Func<Cell, bool>>? expr) =>
                expr is null ? cells.AsQueryable() : cells.AsQueryable().Where(expr));
        cellRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<Cell>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Cell> q, CancellationToken _) => q.FirstOrDefault());

        // CellSector repository
        var cellSectorRepoMock = new Mock<ICellSectorRepository>();
        cellSectorRepoMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<CellSector, bool>>?>()))
            .Returns((System.Linq.Expressions.Expression<Func<CellSector, bool>>? expr) =>
                expr is null ? sectors.AsQueryable() : sectors.AsQueryable().Where(expr));
        cellSectorRepoMock
            .Setup(x => x.ToListAsync(It.IsAny<IQueryable<CellSector>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<CellSector> q, CancellationToken _) => q.ToList());

        // Cellular repository — mutable so Add/Delete mutate the in-memory list
        var currentCellulars = new List<Cellular>(cellulars);
        var cellularRepoMock = new Mock<IRepository<Cellular>>();
        cellularRepoMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Cellular, bool>>?>()))
            .Returns((System.Linq.Expressions.Expression<Func<Cellular, bool>>? expr) =>
                expr is null ? currentCellulars.AsQueryable() : currentCellulars.AsQueryable().Where(expr));

        // Handles both ToListAsync<Cellular> and ToListAsync<{anonymous projection}>
        cellularRepoMock
            .Setup(x => x.ToListAsync(It.IsAny<IQueryable<It.IsAnyType>>(), It.IsAny<CancellationToken>()))
            .Returns(new InvocationFunc(invocation =>
            {
                var tResult = invocation.Method.GetGenericArguments()[0];
                var queryable = (System.Collections.IEnumerable)invocation.Arguments[0];
                var toList = typeof(Enumerable)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == nameof(Enumerable.ToList) && m.GetParameters().Length == 1)
                    .MakeGenericMethod(tResult);
                var list = toList.Invoke(null, [queryable])!;
                return typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(typeof(List<>).MakeGenericType(tResult))
                    .Invoke(null, [list])!;
            }));

        cellularRepoMock
            .Setup(x => x.Add(It.IsAny<Cellular>()))
            .Callback<Cellular>(c => { currentCellulars.Add(c); onAdd?.Invoke(c); });
        cellularRepoMock
            .Setup(x => x.Delete(It.IsAny<Cellular>()))
            .Callback<Cellular>(c => { currentCellulars.Remove(c); onDelete?.Invoke(c); });
        cellularRepoMock
            .Setup(x => x.RemoveRange(It.IsAny<IEnumerable<Cellular>>()))
            .Callback<IEnumerable<Cellular>>(items =>
            {
                foreach (var item in items)
                    currentCellulars.Remove(item);
            });
        var cellularSaveChangesCalls = 0;
        cellularRepoMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                cellularSaveChangesCalls++;
                if (simulateConcurrentUniqueViolationOnCellularSave && cellularSaveChangesCalls == 1)
                {
                    currentCellulars.Add(MakeCellular());
                    throw CreateUniqueViolationException();
                }

                return Task.CompletedTask;
            });

        // Technology repository
        var techRepoMock = new Mock<ITechnologyRepository>();
        techRepoMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Technology, bool>>?>()))
            .Returns((System.Linq.Expressions.Expression<Func<Technology, bool>>? expr) =>
                expr is null ? technologies.AsQueryable() : technologies.AsQueryable().Where(expr));
        techRepoMock
            .Setup(x => x.ToListAsync(It.IsAny<IQueryable<Technology>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Technology> q, CancellationToken _) => q.ToList());

        // Band repository
        var bandRepoMock = new Mock<IBandRepository>();
        bandRepoMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Band, bool>>?>()))
            .Returns((System.Linq.Expressions.Expression<Func<Band, bool>>? expr) =>
                expr is null ? bands.AsQueryable() : bands.AsQueryable().Where(expr));
        bandRepoMock
            .Setup(x => x.ToListAsync(It.IsAny<IQueryable<Band>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Band> q, CancellationToken _) => q.ToList());

        // ServiceProvider repository
        var spRepoMock = new Mock<IServiceProviderRepository>();
        spRepoMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<ServiceProvider, bool>>?>()))
            .Returns((System.Linq.Expressions.Expression<Func<ServiceProvider, bool>>? expr) =>
                expr is null ? serviceProviders.AsQueryable() : serviceProviders.AsQueryable().Where(expr));
        spRepoMock
            .Setup(x => x.CountAsync(It.IsAny<IQueryable<ServiceProvider>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<ServiceProvider> q, CancellationToken _) => q.Count());

        // NetworkService mock
        // Returns Guid.Empty when no SystemType exists (simulates SystemType not found),
        // returns existing network's ID when networks are provided, otherwise returns NetworkId.
        var networkServiceReturnValue = (systemTypes.Count == 0)
            ? Guid.Empty
            : (networks.Count > 0 ? networks[0].Id : NetworkId);
        var networkServiceMock = new Mock<INetworkService>();
        networkServiceMock
            .Setup(x => x.GetOrCreateNetworkAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(networkServiceReturnValue);

        // Sector repository — uses batch ToListAsync for existence check
        var currentSectors = new List<Sector>(existingSectors);
        var sectorRepoMock = new Mock<ISectorRepository>();
        sectorRepoMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Sector, bool>>?>()))
            .Returns((System.Linq.Expressions.Expression<Func<Sector, bool>>? expr) =>
                expr is null ? currentSectors.AsQueryable() : currentSectors.AsQueryable().Where(expr));
        sectorRepoMock
            .Setup(x => x.ToListAsync(It.IsAny<IQueryable<It.IsAnyType>>(), It.IsAny<CancellationToken>()))
            .Returns(new InvocationFunc(invocation =>
            {
                var tResult = invocation.Method.GetGenericArguments()[0];
                var queryable = (System.Collections.IEnumerable)invocation.Arguments[0];
                var toList = typeof(Enumerable)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == nameof(Enumerable.ToList) && m.GetParameters().Length == 1)
                    .MakeGenericMethod(tResult);
                var list = toList.Invoke(null, [queryable])!;
                return typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(typeof(List<>).MakeGenericType(tResult))
                    .Invoke(null, [list])!;
            }));
        sectorRepoMock
            .Setup(x => x.Add(It.IsAny<Sector>()))
            .Callback<Sector>(s => onAddSector?.Invoke(s));
        sectorRepoMock
            .Setup(x => x.AddRange(It.IsAny<IEnumerable<Sector>>()))
            .Callback<IEnumerable<Sector>>(sectorsToAdd =>
            {
                foreach (var sector in sectorsToAdd)
                    onAddSector?.Invoke(sector);
            });
        sectorRepoMock
            .Setup(x => x.RemoveRange(It.IsAny<IEnumerable<Sector>>()));
        sectorRepoMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => onSectorSaveChanges?.Invoke())
            .Returns(Task.CompletedTask);

        // Municipality repository
        var municipalityRepoMock = new Mock<IMunicipalityRepository>();
        municipalityRepoMock
            .Setup(x => x.GetMunicipalityIdAndNameByLocationAsync(
                It.IsAny<Point>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MunicipalityId, "TestMunicipality"));

        return new UpdateCellTechnologiesBandHandler(
            cellRepoMock.Object,
            cellSectorRepoMock.Object,
            cellularRepoMock.Object,
            techRepoMock.Object,
            bandRepoMock.Object,
            spRepoMock.Object,
            sectorRepoMock.Object,
            municipalityRepoMock.Object,
            networkServiceMock.Object,
            new Mock<ILogger<UpdateCellTechnologiesBandHandler>>().Object);
    }

    private static DbUpdateException CreateUniqueViolationException() =>
        new(
            "Unique constraint violation",
            new PostgresException(
                "duplicate key value violates unique constraint",
                "ERROR",
                "ERROR",
                PostgresErrorCodes.UniqueViolation));
}
