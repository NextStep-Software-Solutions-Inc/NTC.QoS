using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NTC.QoS.Application.Features.User.Handlers.Commands;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Role;
using NTC.QoS.Domain.Models.DTOs.User;
using System.Linq.Expressions;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Users;

public class InviteUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRepository<Role>> _roleRepositoryMock;
    private readonly Mock<IRepository<Region>> _regionRepositoryMock;
    private readonly Mock<IClerkUserService> _clerkUserServiceMock;
    private readonly Mock<IUserAuthorizationService> _userAuthorizationServiceMock;
    private readonly IConfiguration _configuration;
    private readonly InviteUserHandler _handler;

    public InviteUserHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRepository<Role>>();
        _regionRepositoryMock = new Mock<IRepository<Region>>();
        _clerkUserServiceMock = new Mock<IClerkUserService>();
        _userAuthorizationServiceMock = new Mock<IUserAuthorizationService>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Clerk:FrontendUrl", "http://localhost:3000/select-region" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var caller = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = "caller@example.com",
            AuthUserId = "user_caller",
            Role = new RoleDto
            {
                Id = Guid.NewGuid(),
                Key = Roles.SuperAdmin,
                Name = "Super Admin",
                Scope = Roles.ScopePlatform
            }
        };
        _userAuthorizationServiceMock.Setup(a => a.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(caller);

        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync(It.IsAny<string?>()))
            .ReturnsAsync(new List<ClerkInvitationResponse>());

        _handler = new InviteUserHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _regionRepositoryMock.Object,
            _clerkUserServiceMock.Object,
            _configuration,
            _userAuthorizationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidInvite_CallsClerkCreateInvitationAndDoesNotInsertUser()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var superAdminRole = new Role { Id = roleId, Key = Roles.SuperAdmin, Name = "Super Admin" };

        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { superAdminRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(superAdminRole);

        var request = new InviteUserRequest(new InviteUserDto
        {
            Email = "invited@example.com",
            RoleId = roleId,
            RedirectUrl = "http://localhost:3000/select-region"
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Contain("Invitation sent successfully via Clerk");
        _clerkUserServiceMock.Verify(c => c.CreateInvitationAsync(It.Is<CreateClerkInvitationPayload>(p =>
            p.EmailAddress == "invited@example.com" &&
            p.RedirectUrl == "http://localhost:3000/select-region")), Times.Once);
        _userRepositoryMock.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ReturnsDuplicateError()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            AuthUserId = "user_123"
        };

        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var request = new InviteUserRequest(new InviteUserDto
        {
            Email = "existing@example.com",
            RoleId = Guid.NewGuid()
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("already exists");
        _clerkUserServiceMock.Verify(c => c.CreateInvitationAsync(It.IsAny<CreateClerkInvitationPayload>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithRegionalAssignments_IncludesRegionsInMetadata()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var regionId = Guid.NewGuid();
        var regionalRoleId = Guid.NewGuid();

        var primaryRole = new Role { Id = roleId, Key = Roles.RegionalStaff, Name = "Regional Staff" };
        var regionalRole = new Role { Id = regionalRoleId, Key = Roles.RegionalAdmin, Name = "Regional Admin" };
        var region = new Region { Id = regionId, Name = "Region VII", Code = "R7" };

        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns((Expression<Func<Role, bool>> expr) =>
            {
                var list = new List<Role> { primaryRole, regionalRole };
                return list.AsQueryable().Where(expr);
            });
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Role> q, CancellationToken ct) => q.FirstOrDefault());

        _regionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Region, bool>>>()))
            .Returns(new List<Region> { region }.AsQueryable());
        _regionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Region> { region });

        var request = new InviteUserRequest(new InviteUserDto
        {
            Email = "regional@example.com",
            RoleId = roleId,
            Regions =
            [
                new InviteUserRegionDto
                {
                    RegionId = regionId,
                    RoleId = regionalRoleId,
                    Status = "Active"
                }
            ]
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _clerkUserServiceMock.Verify(c => c.CreateInvitationAsync(It.IsAny<CreateClerkInvitationPayload>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPriorPendingInviteExists_CreatesNewInviteAndRevokesPriorInvite()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var superAdminRole = new Role { Id = roleId, Key = Roles.SuperAdmin, Name = "Super Admin" };

        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { superAdminRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(superAdminRole);

        var priorInvite = new ClerkInvitationResponse
        {
            Id = "inv_prior_123",
            EmailAddress = "test@example.com",
            Status = "pending"
        };
        var newInvite = new ClerkInvitationResponse
        {
            Id = "inv_new_456",
            EmailAddress = "test@example.com",
            Status = "pending"
        };

        _clerkUserServiceMock.Setup(c => c.CreateInvitationAsync(It.IsAny<CreateClerkInvitationPayload>()))
            .ReturnsAsync(newInvite);
        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync("pending"))
            .ReturnsAsync(new List<ClerkInvitationResponse> { priorInvite, newInvite });

        var request = new InviteUserRequest(new InviteUserDto
        {
            Email = "test@example.com",
            RoleId = roleId
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _clerkUserServiceMock.Verify(c => c.CreateInvitationAsync(It.IsAny<CreateClerkInvitationPayload>()), Times.Once);
        _clerkUserServiceMock.Verify(c => c.RevokeInvitationAsync("inv_prior_123"), Times.Once);
        _clerkUserServiceMock.Verify(c => c.RevokeInvitationAsync("inv_new_456"), Times.Never);
    }
}
