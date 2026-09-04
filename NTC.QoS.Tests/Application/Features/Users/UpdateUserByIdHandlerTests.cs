using FluentAssertions;
using Moq;
using NTC.QoS.Application.Features.User.Handlers.Commands;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.User;
using System.Linq.Expressions;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Users;

public class UpdateUserByIdHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRepository<Role>> _roleRepositoryMock;
    private readonly Mock<IRepository<Organization>> _organizationRepositoryMock;
    private readonly Mock<IUserRegionService> _userRegionServiceMock;
    private readonly UpdateUserByIdHandler _handler;

    public UpdateUserByIdHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRepository<Role>>();
        _organizationRepositoryMock = new Mock<IRepository<Organization>>();
        _userRegionServiceMock = new Mock<IUserRegionService>();

        _handler = new UpdateUserByIdHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _organizationRepositoryMock.Object,
            _userRegionServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserUpdatedToSuperAdmin_CallsAssignAllRegionsToUserAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var superAdminRoleId = Guid.NewGuid();
        var superAdminRole = new Role
        {
            Id = superAdminRoleId,
            Key = Roles.SuperAdmin,
            Name = "Super Admin"
        };

        var existingUser = new User
        {
            Id = userId,
            RoleId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        var users = new List<User> { existingUser };

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns<Expression<Func<User, bool>>>(expr => users.AsQueryable().Where(expr));
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<User> q, CancellationToken ct) => q.FirstOrDefault());

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { superAdminRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(superAdminRole);

        var request = new UpdateUserByIdRequest(userId, new UpdateUserDto
        {
            RoleId = superAdminRoleId
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userRegionServiceMock.Verify(s => s.AssignAllRegionsToUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _userRegionServiceMock.Verify(s => s.DeactivateAllRegionsForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.FieldOfficer)]
    [InlineData(Roles.User)]
    [InlineData(Roles.Manager)]
    [InlineData(Roles.RegionalManager)]
    [InlineData(Roles.Viewer)]
    public async Task Handle_WhenUserUpdatedToNonSuperAdminRole_CallsDeactivateAllRegionsForUserAsync(string roleKey)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nonSuperAdminRoleId = Guid.NewGuid();
        var nonSuperAdminRole = new Role
        {
            Id = nonSuperAdminRoleId,
            Key = roleKey,
            Name = roleKey
        };

        var existingUser = new User
        {
            Id = userId,
            RoleId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "user@example.com"
        };

        var users = new List<User> { existingUser };

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns<Expression<Func<User, bool>>>(expr => users.AsQueryable().Where(expr));
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<User> q, CancellationToken ct) => q.FirstOrDefault());

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { nonSuperAdminRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonSuperAdminRole);

        var request = new UpdateUserByIdRequest(userId, new UpdateUserDto
        {
            RoleId = nonSuperAdminRoleId
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userRegionServiceMock.Verify(s => s.DeactivateAllRegionsForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _userRegionServiceMock.Verify(s => s.AssignAllRegionsToUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserUpdatedWithoutRoleChange_DoesNotModifyUserRegions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = userId,
            RoleId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "user@example.com",
            FirstName = "OldName"
        };

        var users = new List<User> { existingUser };

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns<Expression<Func<User, bool>>>(expr => users.AsQueryable().Where(expr));
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<User> q, CancellationToken ct) => q.FirstOrDefault());

        var request = new UpdateUserByIdRequest(userId, new UpdateUserDto
        {
            FirstName = "NewName"
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userRegionServiceMock.Verify(s => s.AssignAllRegionsToUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRegionServiceMock.Verify(s => s.DeactivateAllRegionsForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
