using FluentAssertions;
using Moq;
using NTC.QoS.Application.Features.Role.Handlers.Commands;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Role;
using System.Linq.Expressions;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.RoleTests;

public class UpdateRolePermissionsHandlerTests
{
    private readonly Mock<IRepository<Role>> _roleRepositoryMock;
    private readonly Mock<IUserAuthorizationService> _userAuthServiceMock;
    private readonly UpdateRolePermissionsHandler _handler;

    public UpdateRolePermissionsHandlerTests()
    {
        _roleRepositoryMock = new Mock<IRepository<Role>>();
        _userAuthServiceMock = new Mock<IUserAuthorizationService>();
        _handler = new UpdateRolePermissionsHandler(_roleRepositoryMock.Object, _userAuthServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsError()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role>().AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var request = new UpdateRolePermissionsRequest(roleId, new UpdateRolePermissionsDto
        {
            Permissions = [Permissions.ViewUsers]
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Role not found");
    }

    [Fact]
    public async Task Handle_WhenSystemRoleAndCallerNotSuperAdmin_BlocksUpdate()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var systemRole = new Role
        {
            Id = roleId,
            Key = Roles.RegionalAdmin,
            Name = "Regional Admin",
            IsSystemRole = true,
            Permissions = [Permissions.ViewUsers]
        };

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { systemRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(systemRole);

        _userAuthServiceMock.Setup(a => a.HasRoleAsync(Roles.SuperAdmin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new UpdateRolePermissionsRequest(roleId, new UpdateRolePermissionsDto
        {
            Permissions = [Permissions.ViewUsers, Permissions.CreateUsers]
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Only Super Administrators can modify permissions of system roles");
        _roleRepositoryMock.Verify(r => r.Update(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvalidPermissionKeyProvided_ReturnsError()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var customRole = new Role
        {
            Id = roleId,
            Key = "custom_role",
            Name = "Custom Role",
            IsSystemRole = false,
            Scope = Roles.ScopeRegional,
            RegionId = Guid.NewGuid()
        };

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { customRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customRole);

        var request = new UpdateRolePermissionsRequest(roleId, new UpdateRolePermissionsDto
        {
            Permissions = ["invalid:fake:permission"]
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Invalid permission(s)");
        _roleRepositoryMock.Verify(r => r.Update(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRegionalRoleAssignedPlatformPermission_ReturnsError()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var customRole = new Role
        {
            Id = roleId,
            Key = "custom_regional",
            Name = "Custom Regional",
            IsSystemRole = false,
            Scope = Roles.ScopeRegional,
            RegionId = Guid.NewGuid()
        };

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { customRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customRole);

        var request = new UpdateRolePermissionsRequest(roleId, new UpdateRolePermissionsDto
        {
            Permissions = [Permissions.SystemAdministration] // Platform-only permission!
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("cannot be assigned to regional roles");
        _roleRepositoryMock.Verify(r => r.Update(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidPermissionsProvided_UpdatesSuccessfully()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var customRole = new Role
        {
            Id = roleId,
            Key = "custom_regional",
            Name = "Custom Regional",
            IsSystemRole = false,
            Scope = Roles.ScopeRegional,
            RegionId = Guid.NewGuid(),
            Permissions = [Permissions.ViewUsers]
        };

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { customRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customRole);

        var newPermissions = new List<string> { Permissions.ViewUsers, Permissions.ViewReports, Permissions.ViewAnalytics };
        var request = new UpdateRolePermissionsRequest(roleId, new UpdateRolePermissionsDto
        {
            Permissions = newPermissions
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Permissions.Should().BeEquivalentTo(newPermissions);
        customRole.Permissions.Should().BeEquivalentTo(newPermissions);
        _roleRepositoryMock.Verify(r => r.Update(customRole), Times.Once);
        _roleRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
