using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Cells.Handlers.Commands;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Application.Helpers;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Cell;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Cells.Handlers.Commands;

public class UpdateCellByIdHandlerNameSyncTests
{
    private readonly Mock<ICellRepository> _cellRepositoryMock = new();
    private readonly Mock<IServiceProviderRepository> _serviceProviderRepositoryMock = new();
    private readonly Mock<IConnectivityAssessmentTriggerService> _connectivityAssessmentTriggerServiceMock = new();
    private readonly UpdateCellByIdHandler _handler;

    public UpdateCellByIdHandlerNameSyncTests()
    {
        _handler = new UpdateCellByIdHandler(
            _cellRepositoryMock.Object,
            _serviceProviderRepositoryMock.Object,
            _connectivityAssessmentTriggerServiceMock.Object,
            Mock.Of<ILogger<UpdateCellByIdHandler>>());
    }

    [Fact]
    public async Task Handle_ShouldAutoUpdateTemplateName_WhenTemplateParametersAreUpdatedAndNameIsNotProvided()
    {
        var cellId = Guid.NewGuid();
        var providerName = "GLOBE COMMUNICATIONS INC.";
        var address = "PUROK 6, POBLACION";
        var existingCell = new Cell
        {
            Id = cellId,
            Group = providerName,
            Address = address,
            TowerType = TowerTypeEnum.Unknown,
            Location = GeometryHelper.CreatePoint(new Coordinate(y: 8.051865, x: 123.794572)),
            Name = CellNameHelper.BuildTemplateName(providerName, address, TowerTypeEnum.Unknown.ToString(), 123.794572, 8.051865),
            ServiceProviderId = Guid.NewGuid()
        };

        _cellRepositoryMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Cell, bool>>>()))
            .Returns(new List<Cell> { existingCell }.AsQueryable());
        _cellRepositoryMock
            .Setup(x => x.QueryAsNoTracking(It.IsAny<System.Linq.Expressions.Expression<Func<Cell, bool>>>()))
            .Returns(new List<Cell> { existingCell }.AsQueryable());
        _cellRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<Cell>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCell);
        _cellRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.Empty);
        _cellRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<CellDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<CellDto> q, CancellationToken _) => q.FirstOrDefault());
        _cellRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _serviceProviderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerName);
        _connectivityAssessmentTriggerServiceMock
            .Setup(x => x.TriggerAssessmentAsync(It.IsAny<Point>(), cellId, "Cell", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        const double updatedLatitude = 9.123456;
        const double updatedLongitude = 124.654321;
        var request = new UpdateCellByIdRequest(
            cellId,
            new UpdateCellDto
            {
                Latitude = updatedLatitude,
                Longitude = updatedLongitude
            });

        var result = await _handler.Handle(request, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(CellNameHelper.BuildTemplateName(providerName, address, TowerTypeEnum.Unknown.ToString(), updatedLongitude, updatedLatitude));
    }

    [Fact]
    public async Task Handle_ShouldKeepManualName_WhenNameAndTemplateParametersAreUpdatedTogether()
    {
        var cellId = Guid.NewGuid();
        var providerName = "GLOBE COMMUNICATIONS INC.";
        var address = "PUROK 6, POBLACION";
        var existingCell = new Cell
        {
            Id = cellId,
            Group = providerName,
            Address = address,
            TowerType = TowerTypeEnum.Unknown,
            Location = GeometryHelper.CreatePoint(new Coordinate(y: 8.051865, x: 123.794572)),
            Name = CellNameHelper.BuildTemplateName(providerName, address, TowerTypeEnum.Unknown.ToString(), 123.794572, 8.051865),
            ServiceProviderId = Guid.NewGuid()
        };

        _cellRepositoryMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Cell, bool>>>()))
            .Returns(new List<Cell> { existingCell }.AsQueryable());
        _cellRepositoryMock
            .Setup(x => x.QueryAsNoTracking(It.IsAny<System.Linq.Expressions.Expression<Func<Cell, bool>>>()))
            .Returns(new List<Cell> { existingCell }.AsQueryable());
        _cellRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<Cell>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCell);
        _cellRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.Empty);
        _cellRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<CellDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<CellDto> q, CancellationToken _) => q.FirstOrDefault());
        _cellRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _connectivityAssessmentTriggerServiceMock
            .Setup(x => x.TriggerAssessmentAsync(It.IsAny<Point>(), cellId, "Cell", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manualName = "Manual Cell Name";
        var request = new UpdateCellByIdRequest(
            cellId,
            new UpdateCellDto
            {
                Name = manualName,
                TowerType = TowerTypeEnum.Unknown.ToString()
            });

        var result = await _handler.Handle(request, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(manualName);
    }
}
