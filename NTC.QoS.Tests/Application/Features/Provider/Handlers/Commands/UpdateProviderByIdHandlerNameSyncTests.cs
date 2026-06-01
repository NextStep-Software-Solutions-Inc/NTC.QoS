using FluentAssertions;
using FluentValidation;
using Moq;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Provider.Handlers.Commands;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Application.Helpers;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Provider;
using ProviderEntity = NTC.QoS.Domain.Entities.Provider;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Provider.Handlers.Commands;

public class UpdateProviderByIdHandlerNameSyncTests
{
    private readonly Mock<IRepository<ProviderEntity>> _providerRepositoryMock = new();
    private readonly Mock<IServiceProviderRepository> _serviceProviderRepositoryMock = new();
    private readonly Mock<ICellRepository> _cellRepositoryMock = new();
    private readonly Mock<IValidator<UpdateProviderByIdRequest>> _validatorMock = new();
    private readonly UpdateProviderByIdHandler _handler;

    public UpdateProviderByIdHandlerNameSyncTests()
    {
        _handler = new UpdateProviderByIdHandler(
            _providerRepositoryMock.Object,
            _validatorMock.Object,
            _serviceProviderRepositoryMock.Object,
            _cellRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSyncOnlyTemplateBasedCellNames_WhenProviderNameChanges()
    {
        var providerId = Guid.NewGuid();
        var serviceProviderId = Guid.NewGuid();
        var oldProviderName = "Old Provider";
        var newProviderName = "New Provider";
        var address = "POBLACION";
        var templateCell = new Cell
        {
            Id = Guid.NewGuid(),
            ServiceProviderId = serviceProviderId,
            Address = address,
            TowerType = TowerTypeEnum.Unknown,
            Location = GeometryHelper.CreatePoint(new Coordinate(y: 8.051865, x: 123.794572)),
            Name = CellNameHelper.BuildTemplateName(oldProviderName, address, TowerTypeEnum.Unknown.ToString(), 123.794572, 8.051865)
        };
        var manualCell = new Cell
        {
            Id = Guid.NewGuid(),
            ServiceProviderId = serviceProviderId,
            Address = address,
            TowerType = TowerTypeEnum.Unknown,
            Location = GeometryHelper.CreatePoint(new Coordinate(y: 8.051865, x: 123.794572)),
            Name = "Manual Name"
        };
        var provider = new ProviderEntity
        {
            Id = providerId,
            Name = oldProviderName,
            Description = "Desc",
            LicenseNumber = "Lic",
            Color = "#000000"
        };

        _providerRepositoryMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<ProviderEntity, bool>>>()))
            .Returns(new List<ProviderEntity> { provider }.AsQueryable());
        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<ProviderEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);
        _providerRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<UpdateProviderByIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _serviceProviderRepositoryMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<ServiceProvider, bool>>>()))
            .Returns(new List<ServiceProvider>
            {
                new() { Id = serviceProviderId, ProviderId = providerId }
            }.AsQueryable());
        _cellRepositoryMock
            .Setup(x => x.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Cell, bool>>>()))
            .Returns(new List<Cell> { templateCell, manualCell }.AsQueryable());
        _cellRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new UpdateProviderByIdRequest(
            providerId,
            new UpdateProviderDto
            {
                Name = newProviderName,
                Description = "Desc",
                LicenseNumber = "Lic",
                Color = "#000000"
            });

        var result = await _handler.Handle(request, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _cellRepositoryMock.Verify(x => x.Update(It.Is<Cell>(cell =>
            cell.Id == templateCell.Id
            && cell.Name == CellNameHelper.BuildTemplateName(newProviderName, address, TowerTypeEnum.Unknown.ToString(), 123.794572, 8.051865))), Times.Once);
        manualCell.Name.Should().Be("Manual Name");
    }
}
