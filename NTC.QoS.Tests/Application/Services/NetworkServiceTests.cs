using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using Xunit;

namespace NTC.QoS.Tests.Application.Services;

public class NetworkServiceTests
{
    [Fact]
    public async Task GetOrCreateNetworkAsync_WhenExistingNameHasDifferentCaseAndWhitespace_ReusesExistingNetwork()
    {
        var serviceProviderId = Guid.NewGuid();
        var existingNetwork = new Network
        {
            Id = Guid.NewGuid(),
            Name = " LTE_Band 3_TestMunicipality ",
            ServiceProviderId = serviceProviderId
        };

        var serviceContext = CreateService(
            [existingNetwork],
            [new SystemType { Id = Guid.NewGuid(), TechnologyId = Guid.NewGuid(), Name = "LTE" }]);

        var result = await serviceContext.Service.GetOrCreateNetworkAsync(
            serviceProviderId,
            Guid.NewGuid(),
            "TestMunicipality",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LTE",
            "Band 3",
            1842.5m,
            1747.5m,
            20m,
            CancellationToken.None);

        result.Should().Be(existingNetwork.Id);
        serviceContext.NetworkRepositoryMock.Verify(x => x.Add(It.IsAny<Network>()), Times.Never);
        serviceContext.NetworkRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateNetworkAsync_WhenUniqueViolationOccurs_RequeriesAndReturnsWinningNetwork()
    {
        var serviceProviderId = Guid.NewGuid();
        var technologyId = Guid.NewGuid();
        var bandId = Guid.NewGuid();
        var winnerNetwork = new Network
        {
            Id = Guid.NewGuid(),
            Name = "lte_band 3_testmunicipality",
            ServiceProviderId = serviceProviderId
        };

        var saveChangesCalled = 0;
        var serviceContext = CreateService(
            [],
            [new SystemType { Id = Guid.NewGuid(), TechnologyId = technologyId, Name = "LTE" }],
            (networks) =>
            {
                saveChangesCalled++;
                if (saveChangesCalled == 1)
                {
                    networks.Add(winnerNetwork);
                    throw CreateUniqueViolationException();
                }
            });

        var result = await serviceContext.Service.GetOrCreateNetworkAsync(
            serviceProviderId,
            Guid.NewGuid(),
            "TestMunicipality",
            technologyId,
            bandId,
            "LTE",
            "Band 3",
            1842.5m,
            1747.5m,
            20m,
            CancellationToken.None);

        result.Should().Be(winnerNetwork.Id);
        saveChangesCalled.Should().Be(1);
        serviceContext.NetworkRepositoryMock.Verify(x => x.Delete(It.IsAny<Network>()), Times.Once);
    }

    private static (NetworkService Service, Mock<INetworkRepository> NetworkRepositoryMock) CreateService(
        List<Network> initialNetworks,
        List<SystemType> systemTypes,
        Action<List<Network>>? saveChangesAction = null)
    {
        var networks = new List<Network>(initialNetworks);
        var networkRepositoryMock = new Mock<INetworkRepository>();
        networkRepositoryMock
            .Setup(x => x.Query(It.IsAny<Expression<Func<Network, bool>>?>()))
            .Returns((Expression<Func<Network, bool>>? expression) =>
                expression is null ? networks.AsQueryable() : networks.AsQueryable().Where(expression));
        networkRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<Guid?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Guid?> query, CancellationToken _) => query.FirstOrDefault());
        networkRepositoryMock
            .Setup(x => x.Add(It.IsAny<Network>()))
            .Callback<Network>(network => networks.Add(network));
        networkRepositoryMock
            .Setup(x => x.Delete(It.IsAny<Network>()))
            .Callback<Network>(network => networks.Remove(network));
        networkRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                saveChangesAction?.Invoke(networks);
                return Task.CompletedTask;
            });

        var systemTypeRepositoryMock = new Mock<IRepository<SystemType>>();
        systemTypeRepositoryMock
            .Setup(x => x.Query(It.IsAny<Expression<Func<SystemType, bool>>?>()))
            .Returns((Expression<Func<SystemType, bool>>? expression) =>
                expression is null ? systemTypes.AsQueryable() : systemTypes.AsQueryable().Where(expression));
        systemTypeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<Guid?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Guid?> query, CancellationToken _) => query.FirstOrDefault());

        var loggerMock = new Mock<ILogger<NetworkService>>();
        var service = new NetworkService(networkRepositoryMock.Object, systemTypeRepositoryMock.Object, loggerMock.Object);

        return (service, networkRepositoryMock);
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
