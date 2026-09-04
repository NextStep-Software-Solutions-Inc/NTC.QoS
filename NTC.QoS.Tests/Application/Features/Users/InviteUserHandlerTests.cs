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

public class InviteUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRepository<Role>> _roleRepositoryMock;
    private readonly Mock<IRepository<Organization>> _organizationRepositoryMock;
    private readonly Mock<IUserRegionRepository> _userRegionRepositoryMock;
    private readonly Mock<IRepository<Region>> _regionRepositoryMock;
    private readonly InviteUserHandler _handler;

    public InviteUserHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRepository<Role>>();
        _organizationRepositoryMock = new Mock<IRepository<Organization>>();
        _userRegionRepositoryMock = new Mock<IUserRegionRepository>();
        _regionRepositoryMock = new Mock<IRepository<Region>>();

        _handler = new InviteUserHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _organizationRepositoryMock.Object,
            _userRegionRepositoryMock.Object,
            _regionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSuperAdminInvited_AutoAssignsAllActiveRegions()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var superAdminRole = new Role { Id = roleId, Key = Roles.SuperAdmin, Name = "Super Admin" };
        var organization = new Organization { Id = orgId, Name = "National" };

        var region1 = new Region { Id = Guid.NewGuid(), Name = "NCR", Code = "NCR", IsDeleted = false };
        var region2 = new Region { Id = Guid.NewGuid(), Name = "Region VII", Code = "R07", IsDeleted = false };
        var allRegions = new List<Region> { region1, region2 };

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

        _regionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Region, bool>>>()))
            .Returns(allRegions.AsQueryable());
        _regionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allRegions);

        _userRegionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<UserRegion, bool>>>()))
            .Returns(new List<UserRegion>().AsQueryable());
        _userRegionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<UserRegion>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRegion>());

        List<UserRegion>? addedUserRegions = null;
        _userRegionRepositoryMock.Setup(r => r.AddRange(It.IsAny<IEnumerable<UserRegion>>()))
            .Callback<IEnumerable<UserRegion>>(list => addedUserRegions = list.ToList());

        var request = new InviteUserRequest(new InviteUserDto
        {
            Email = "superadmin@example.com",
            RoleId = roleId,
            OrganizationId = orgId
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userRegionRepositoryMock.Verify(r => r.AddRange(It.IsAny<IEnumerable<UserRegion>>()), Times.Once);
        addedUserRegions.Should().NotBeNull();
        addedUserRegions!.Count.Should().Be(2);
        addedUserRegions.Should().OnlyContain(ur => ur.Status == UserRegionStatus.Active.ToString());
    }
}
