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
    private readonly Mock<IRepository<Organization>> _organizationRepositoryMock;
    private readonly Mock<IUserRegionService> _userRegionServiceMock;
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTests()
    {
        _userRepositoryMock = new Mock<IRepository<User>>();
        _roleRepositoryMock = new Mock<IRepository<Role>>();
        _organizationRepositoryMock = new Mock<IRepository<Organization>>();
        _userRegionServiceMock = new Mock<IUserRegionService>();

        _handler = new CreateUserHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _organizationRepositoryMock.Object,
            _userRegionServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSuperAdminCreated_CallsUserRegionService()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var superAdminRole = new Role { Id = roleId, Key = Roles.SuperAdmin, Name = "Super Admin" };
        var organization = new Organization { Id = orgId, Name = "National" };

        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { superAdminRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(superAdminRole);

        _organizationRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Organization, bool>>>()))
            .Returns(new List<Organization> { organization }.AsQueryable());
        _organizationRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Organization>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var request = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = "clerk_123",
            Email = "superadmin@example.com",
            RoleId = roleId,
            OrganizationId = orgId
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userRegionServiceMock.Verify(s => s.AssignAllRegionsToUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
