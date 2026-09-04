using FluentAssertions;
using Moq;
using NTC.QoS.Application.Features.User.Handlers.Commands;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
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
    private readonly Mock<IUserRegionRepository> _userRegionRepositoryMock;
    private readonly Mock<IRepository<Region>> _regionRepositoryMock;
    private readonly UpdateUserByIdHandler _handler;

    public UpdateUserByIdHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRepository<Role>>();
        _organizationRepositoryMock = new Mock<IRepository<Organization>>();
        _userRegionRepositoryMock = new Mock<IUserRegionRepository>();
        _regionRepositoryMock = new Mock<IRepository<Region>>();

        _handler = new UpdateUserByIdHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _organizationRepositoryMock.Object,
            _userRegionRepositoryMock.Object,
            _regionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserUpdatedToSuperAdmin_AutoAssignsAllActiveRegions()
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

        var region1 = new Region { Id = Guid.NewGuid(), Name = "NCR", Code = "NCR", IsDeleted = false };
        var region2 = new Region { Id = Guid.NewGuid(), Name = "Region VII", Code = "R07", IsDeleted = false };
        var allRegions = new List<Region> { region1, region2 };

        var existingUserRegion = new UserRegion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RegionId = region1.Id,
            Status = UserRegionStatus.Pending.ToString(),
            IsDeleted = false
        };

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns<Expression<Func<User, bool>>>(expr => users.AsQueryable().Where(expr));
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<User> q, CancellationToken ct) => q.FirstOrDefault());

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { superAdminRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(superAdminRole);

        _regionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Region, bool>>>()))
            .Returns(allRegions.AsQueryable());
        _regionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allRegions);

        _userRegionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<UserRegion, bool>>>()))
            .Returns(new List<UserRegion> { existingUserRegion }.AsQueryable());
        _userRegionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<UserRegion>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRegion> { existingUserRegion });

        List<UserRegion>? addedUserRegions = null;
        _userRegionRepositoryMock.Setup(r => r.AddRange(It.IsAny<IEnumerable<UserRegion>>()))
            .Callback<IEnumerable<UserRegion>>(list => addedUserRegions = list.ToList());

        var request = new UpdateUserByIdRequest(userId, new UpdateUserDto
        {
            RoleId = superAdminRoleId
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        existingUserRegion.Status.Should().Be(UserRegionStatus.Active.ToString());
        _userRegionRepositoryMock.Verify(r => r.Update(existingUserRegion), Times.Once);
        _userRegionRepositoryMock.Verify(r => r.AddRange(It.IsAny<IEnumerable<UserRegion>>()), Times.Once);
        addedUserRegions.Should().NotBeNull();
        addedUserRegions!.Should().ContainSingle(ur => ur.RegionId == region2.Id && ur.Status == UserRegionStatus.Active.ToString());
    }

    [Fact]
    public async Task Handle_WhenUserUpdatedToRegularRole_DoesNotAutoAssignRegions()
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
        _regionRepositoryMock.Verify(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRegionRepositoryMock.Verify(r => r.AddRange(It.IsAny<IEnumerable<UserRegion>>()), Times.Never);
    }
}
