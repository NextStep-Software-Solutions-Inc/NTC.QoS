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

public class CreateUserHandlerTests
{
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IRepository<Role>> _roleRepositoryMock;
    private readonly Mock<IUserRegionRepository> _userRegionRepositoryMock;
    private readonly Mock<IUserRegionService> _userRegionServiceMock;
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTests()
    {
        _userRepositoryMock = new Mock<IRepository<User>>();
        _roleRepositoryMock = new Mock<IRepository<Role>>();
        _userRegionRepositoryMock = new Mock<IUserRegionRepository>();
        _userRegionServiceMock = new Mock<IUserRegionService>();

        _handler = new CreateUserHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _userRegionServiceMock.Object,
            _userRegionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSuperAdminCreated_CallsUserRegionService()
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

        var request = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = "clerk_123",
            Email = "superadmin@example.com",
            RoleId = roleId
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userRegionServiceMock.Verify(s => s.AssignAllRegionsToUserAsync(It.IsAny<Guid>(), roleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRegionalUserWithRegionsCreated_AddsUserRegions()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var regionId = Guid.NewGuid();
        var staffRole = new Role { Id = roleId, Key = Roles.RegionalStaff, Name = "Regional Staff" };

        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { staffRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(staffRole);

        var request = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = "clerk_456",
            Email = "staff@example.com",
            RoleId = roleId,
            Regions =
            [
                new CreateUserRegionAssignmentDto
                {
                    RegionId = regionId,
                    RoleId = roleId,
                    Status = "Active"
                }
            ]
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userRegionRepositoryMock.Verify(r => r.AddRange(It.Is<IEnumerable<NTC.QoS.Domain.Aggregates.UserRegion>>(regions =>
            regions.Any(ur => ur.RegionId == regionId && ur.RoleId == roleId))), Times.Once);
    }
}
