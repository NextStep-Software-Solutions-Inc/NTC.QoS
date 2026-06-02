using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Cells.Handlers.Commands;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Cell;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Cells.Handlers;

public class UpdateCellByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenEditableFieldsAreProvided_UpdatesCellAndRegeneratesIdentifierLikeImport()
    {
        var serviceProvider1 = new ServiceProvider
        {
            Id = Guid.NewGuid(),
            Provider = new Provider { Id = Guid.NewGuid(), Name = "Globe", Color = "#111111" },
            Service = new Service { Id = Guid.NewGuid(), Name = "Wireless", Type = ServiceTypeEnum.Wireless }
        };
        var serviceProvider2 = new ServiceProvider
        {
            Id = Guid.NewGuid(),
            Provider = new Provider { Id = Guid.NewGuid(), Name = "Smart", Color = "#222222" },
            Service = new Service { Id = Guid.NewGuid(), Name = "Wireless", Type = ServiceTypeEnum.Wireless }
        };

        var cell = new Cell
        {
            Id = Guid.NewGuid(),
            Name = "Old-Cell",
            Group = "Old Group",
            Address = "Old Address",
            ServiceProviderId = serviceProvider1.Id,
            ServiceProvider = serviceProvider1,
            TowerType = TowerTypeEnum.SelfSupporting,
            Location = GeometryHelper.CreatePoint(new Coordinate(x: 121.0, y: 14.0)),
            CID = "1",
            TAC = "2",
            SiteId = "SITE-1",
            Status = CellStatusEnum.Licensed
        };

        var cells = new List<Cell> { cell };
        var serviceProviders = new List<ServiceProvider> { serviceProvider1, serviceProvider2 };
        var handler = CreateHandler(cells, serviceProviders, out var connectivityTriggerServiceMock, out _, out _);

        var request = new UpdateCellByIdRequest(cell.Id, new UpdateCellDto
        {
            ServiceProviderId = serviceProvider2.Id,
            Group = "Updated Group",
            Address = "Updated Address",
            Latitude = 15.0,
            Longitude = 122.0,
            TowerType = "SelfSupporting"
        });

        var result = await handler.Handle(request, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ServiceProvider!.Id.Should().Be(serviceProvider2.Id);
        result.Data.Group.Should().Be("Updated Group");
        result.Data.Address.Should().Be("Updated Address");
        result.Data.Name.Should().Be("Cell-Smart-Updated Address-SelfSupporting-122-15");

        cell.ServiceProviderId.Should().Be(serviceProvider2.Id);
        cell.Group.Should().Be("Updated Group");
        cell.Address.Should().Be("Updated Address");
        cell.Location.Coordinate.X.Should().Be(122.0);
        cell.Location.Coordinate.Y.Should().Be(15.0);
        cell.Name.Should().Be("Cell-Smart-Updated Address-SelfSupporting-122-15");

        connectivityTriggerServiceMock.Verify(x => x.TriggerAssessmentAsync(
            It.Is<Point>(p => p.Coordinate.X == 122.0 && p.Coordinate.Y == 15.0),
            cell.Id,
            "Cell",
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdentityMatchesAnotherCell_ReturnsDuplicateError()
    {
        var serviceProvider = new ServiceProvider
        {
            Id = Guid.NewGuid(),
            Provider = new Provider { Id = Guid.NewGuid(), Name = "Smart", Color = "#222222" },
            Service = new Service { Id = Guid.NewGuid(), Name = "Wireless", Type = ServiceTypeEnum.Wireless }
        };

        var targetCell = new Cell
        {
            Id = Guid.NewGuid(),
            Name = "Cell-A",
            ServiceProviderId = serviceProvider.Id,
            ServiceProvider = serviceProvider,
            TowerType = TowerTypeEnum.SelfSupporting,
            Location = GeometryHelper.CreatePoint(new Coordinate(x: 121.0, y: 14.0))
        };

        var existingCell = new Cell
        {
            Id = Guid.NewGuid(),
            Name = "Cell-B",
            ServiceProviderId = serviceProvider.Id,
            ServiceProvider = serviceProvider,
            TowerType = TowerTypeEnum.SelfSupporting,
            Location = GeometryHelper.CreatePoint(new Coordinate(x: 122.0, y: 15.0))
        };

        var cells = new List<Cell> { targetCell, existingCell };
        var serviceProviders = new List<ServiceProvider> { serviceProvider };
        var handler = CreateHandler(cells, serviceProviders, out var connectivityTriggerServiceMock, out var cellRepositoryMock, out _);

        var request = new UpdateCellByIdRequest(targetCell.Id, new UpdateCellDto
        {
            Latitude = 15.0,
            Longitude = 122.0,
            TowerType = "SelfSupporting"
        });

        var result = await handler.Handle(request, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("Duplicate");

        cellRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        connectivityTriggerServiceMock.Verify(x => x.TriggerAssessmentAsync(
            It.IsAny<Point>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static UpdateCellByIdHandler CreateHandler(
        List<Cell> cells,
        List<ServiceProvider> serviceProviders,
        out Mock<IConnectivityAssessmentTriggerService> connectivityTriggerServiceMock,
        out Mock<ICellRepository> cellRepositoryMock,
        out Mock<IServiceProviderRepository> serviceProviderRepositoryMock)
    {
        cellRepositoryMock = new Mock<ICellRepository>();
        serviceProviderRepositoryMock = new Mock<IServiceProviderRepository>();
        connectivityTriggerServiceMock = new Mock<IConnectivityAssessmentTriggerService>();
        var loggerMock = new Mock<ILogger<UpdateCellByIdHandler>>();

        cellRepositoryMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Cell, bool>>?>()))
            .Returns((System.Linq.Expressions.Expression<Func<Cell, bool>>? expression) =>
                expression is null ? cells.AsQueryable() : cells.AsQueryable().Where(expression));

        cellRepositoryMock
            .Setup(x => x.QueryAsNoTracking(It.IsAny<System.Linq.Expressions.Expression<Func<Cell, bool>>?>()))
            .Returns((System.Linq.Expressions.Expression<Func<Cell, bool>>? expression) =>
                expression is null ? cells.AsQueryable() : cells.AsQueryable().Where(expression));

        cellRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<Cell>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Cell> query, CancellationToken _) => query.FirstOrDefault());

        cellRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Guid> query, CancellationToken _) => query.FirstOrDefault());

        cellRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<CellDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<CellDto> query, CancellationToken _) => query.FirstOrDefault());

        cellRepositoryMock
            .Setup(x => x.Update(It.IsAny<Cell>()))
            .Callback<Cell>(updatedCell =>
            {
                var index = cells.FindIndex(c => c.Id == updatedCell.Id);
                if (index >= 0)
                {
                    cells[index] = updatedCell;
                }
            });

        cellRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        serviceProviderRepositoryMock
            .Setup(x => x.QueryAsNoTracking(It.IsAny<System.Linq.Expressions.Expression<Func<ServiceProvider, bool>>?>()))
            .Returns((System.Linq.Expressions.Expression<Func<ServiceProvider, bool>>? expression) =>
                expression is null ? serviceProviders.AsQueryable() : serviceProviders.AsQueryable().Where(expression));

        serviceProviderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Guid> query, CancellationToken _) => query.FirstOrDefault());

        serviceProviderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<string> query, CancellationToken _) => query.FirstOrDefault());

        return new UpdateCellByIdHandler(
            cellRepositoryMock.Object,
            serviceProviderRepositoryMock.Object,
            connectivityTriggerServiceMock.Object,
            loggerMock.Object);
    }
}
