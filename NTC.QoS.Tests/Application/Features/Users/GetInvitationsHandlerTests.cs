using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Application.Features.User.Handlers.Queries;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;
using System.Text.Json;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Users;

public class GetInvitationsHandlerTests
{
    private readonly Mock<IClerkUserService> _clerkUserServiceMock;
    private readonly Mock<IRepository<Role>> _roleRepositoryMock;
    private readonly Mock<IRepository<Region>> _regionRepositoryMock;
    private readonly Mock<ILogger<GetInvitationsHandler>> _loggerMock;
    private readonly GetInvitationsHandler _handler;

    public GetInvitationsHandlerTests()
    {
        _clerkUserServiceMock = new Mock<IClerkUserService>();
        _roleRepositoryMock = new Mock<IRepository<Role>>();
        _regionRepositoryMock = new Mock<IRepository<Region>>();
        _loggerMock = new Mock<ILogger<GetInvitationsHandler>>();

        _handler = new GetInvitationsHandler(
            _clerkUserServiceMock.Object,
            _roleRepositoryMock.Object,
            _regionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenInvitationsExist_EnrichesWithRoleAndRegionData()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var regionId = Guid.NewGuid();

        var testRole = new Role { Id = roleId, Name = "Regional Admin", Scope = "Regional", Key = "regional_admin" };
        var testRegion = new Region { Id = regionId, Name = "Region VII", Code = "R-07" };

        var metadataJson = JsonSerializer.SerializeToElement(new
        {
            role_id = roleId.ToString(),
            regions = new[]
            {
                new { region_id = regionId.ToString(), role_id = roleId.ToString(), status = "Active" }
            }
        });

        var clerkInvitations = new List<ClerkInvitationResponse>
        {
            new()
            {
                Id = "inv_123",
                EmailAddress = "officer@example.com",
                Status = "pending",
                Url = "https://clerk.example.com/accept-invitation?token=abc",
                CreatedAt = 1726963200000,
                PublicMetadata = metadataJson
            }
        };

        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync(It.IsAny<string?>()))
            .ReturnsAsync(clerkInvitations);

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { testRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role> { testRole });

        _regionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Region, bool>>>()))
            .Returns(new List<Region> { testRegion }.AsQueryable());
        _regionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Region> { testRegion });

        // Act
        var result = await _handler.Handle(new GetInvitationsRequest(), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().HaveCount(1);

        var first = result.Data!.First();
        first.Id.Should().Be("inv_123");
        first.Email.Should().Be("officer@example.com");
        first.Status.Should().Be("pending");
        first.RoleName.Should().Be("Regional Admin");
        first.RoleScope.Should().Be("Regional");
        first.Regions.Should().HaveCount(1);
        first.Regions.First().RegionName.Should().Be("Region VII");
        first.Regions.First().RegionCode.Should().Be("R-07");
    }

    [Fact]
    public async Task Handle_WhenClerkThrowsException_ReturnsFailureResponse()
    {
        // Arrange
        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync(It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("Network failure to Clerk API"));

        // Act
        var result = await _handler.Handle(new GetInvitationsRequest(), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Network failure to Clerk API");
    }

    [Fact]
    public async Task Handle_WhenInvitationsListIsEmpty_ReturnsEmptyListSuccessfully()
    {
        // Arrange
        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync(It.IsAny<string?>()))
            .ReturnsAsync(new List<ClerkInvitationResponse>());

        // Act
        var result = await _handler.Handle(new GetInvitationsRequest(), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenInvitationsHaveMissingOrMalformedMetadata_HandlesGracefullyWithoutThrowing()
    {
        // Arrange
        var clerkInvitations = new List<ClerkInvitationResponse>
        {
            new()
            {
                Id = "inv_null_meta",
                EmailAddress = "null_meta@example.com",
                Status = "pending",
                PublicMetadata = null
            },
            new()
            {
                Id = "inv_malformed_meta",
                EmailAddress = "malformed_meta@example.com",
                Status = "pending",
                PublicMetadata = JsonSerializer.SerializeToElement("not a valid json object")
            }
        };

        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync(It.IsAny<string?>()))
            .ReturnsAsync(clerkInvitations);

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role>().AsQueryable());
        _roleRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());

        _regionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Region, bool>>>()))
            .Returns(new List<Region>().AsQueryable());
        _regionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Region>());

        // Act
        var result = await _handler.Handle(new GetInvitationsRequest(), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().HaveCount(2);

        var first = result.Data!.First(i => i.Id == "inv_null_meta");
        first.RoleName.Should().BeNull();
        first.Regions.Should().BeEmpty();

        var second = result.Data!.First(i => i.Id == "inv_malformed_meta");
        second.RoleName.Should().BeNull();
        second.Regions.Should().BeEmpty();
    }
}
