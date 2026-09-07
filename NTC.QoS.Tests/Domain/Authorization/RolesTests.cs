using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Entities;
using Xunit;

namespace NTC.QoS.Tests.Domain.Authorization;

public class RolesTests
{
    [Theory]
    [InlineData("super_admin", true)]
    [InlineData("SUPER_ADMIN", true)]
    [InlineData("Super_Admin", true)]
    [InlineData("super-admin", true)]
    [InlineData("SUPER-ADMIN", true)]
    [InlineData("admin", false)]
    [InlineData("field_officer", false)]
    [InlineData("user", false)]
    [InlineData("manager", false)]
    [InlineData("regional_manager", false)]
    [InlineData("viewer", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsSuperAdmin_WithStringRoleKey_ReturnsExpectedResult(string? roleKey, bool expected)
    {
        // Act
        var result = Roles.IsSuperAdmin(roleKey);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSuperAdmin_WithRoleEntity_WhenSuperAdmin_ReturnsTrue()
    {
        // Arrange
        var role = new Role { Id = Guid.NewGuid(), Key = Roles.SuperAdmin, Name = "Super Admin" };

        // Act
        var result = Roles.IsSuperAdmin(role);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSuperAdmin_WithRoleEntity_WhenLegacySuperAdmin_ReturnsTrue()
    {
        // Arrange
#pragma warning disable CS0618
        var role = new Role { Id = Guid.NewGuid(), Key = Roles.SuperAdminLegacy, Name = "Super Admin Legacy" };
#pragma warning restore CS0618

        // Act
        var result = Roles.IsSuperAdmin(role);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSuperAdmin_WithRoleEntity_WhenNull_ReturnsFalse()
    {
        // Act
        var result = Roles.IsSuperAdmin((Role?)null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSuperAdmin_WithRoleEntity_WhenNonSuperAdmin_ReturnsFalse()
    {
        // Arrange
        var role = new Role { Id = Guid.NewGuid(), Key = Roles.Admin, Name = "Admin" };

        // Act
        var result = Roles.IsSuperAdmin(role);

        // Assert
        Assert.False(result);
    }
}
