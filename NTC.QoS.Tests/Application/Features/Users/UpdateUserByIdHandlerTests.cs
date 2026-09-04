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
    public async Task Handle_WhenUserUpdatedToSuperAdmin_CallsUserRegionService()
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
    }

    [Fact]
    public async Task Handle_WhenUserUpdatedToRegularRole_DoesNotCallUserRegionService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var regularRoleId = Guid.NewGuid();
        var regularRole = new Role
        {
            Id = regularRoleId,
            Key = Roles.User,
            Name = "Regular User"
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
            .Returns(new List<Role> { regularRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(regularRole);

        var request = new UpdateUserByIdRequest(userId, new UpdateUserDto
        {
            RoleId = regularRoleId
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userRegionServiceMock.Verify(s => s.AssignAllRegionsToUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
