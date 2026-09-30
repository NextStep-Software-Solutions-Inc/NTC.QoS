using FluentAssertions;
using Moq;
using NTC.QoS.Application.Features.UserRegion.Handlers.Commands;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.UserRegion;
using System.Linq.Expressions;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.UserRegions;

public class UpdateUserRegionStatusByIdHandlerTests
{
    private readonly Mock<IUserRegionRepository> _userRegionRepositoryMock;
    private readonly Mock<IUserContextService> _userContextServiceMock;
    private readonly Mock<IUserAuthorizationService> _userAuthorizationServiceMock;
    private readonly UpdateUserRegionStatusByIdHandler _handler;

    public UpdateUserRegionStatusByIdHandlerTests()
    {
        _userRegionRepositoryMock = new Mock<IUserRegionRepository>();
        _userContextServiceMock = new Mock<IUserContextService>();
        _userAuthorizationServiceMock = new Mock<IUserAuthorizationService>();
        _handler = new UpdateUserRegionStatusByIdHandler(
            _userRegionRepositoryMock.Object,
            _userContextServiceMock.Object,
            _userAuthorizationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserAttemptsSelfApproval_BlocksAndReturnsError()
    {
        // Arrange
        var userRegionId = Guid.NewGuid();
        var callerAuthUserId = "clerk_user_123";

        var targetUser = new User
        {
            Id = Guid.NewGuid(),
            AuthUserId = callerAuthUserId, // Same as caller!
            Email = "user@example.com",
            Role = new Role { Id = Guid.NewGuid(), Key = Roles.RegionalStaff, Name = "Regional Staff" }
        };

        var userRegion = new UserRegion
        {
            Id = userRegionId,
            UserId = targetUser.Id,
            User = targetUser,
            RegionId = Guid.NewGuid(),
            Region = new Region { Id = Guid.NewGuid(), Name = "NCR", Code = "NCR" },
            Status = UserRegionStatus.Pending.ToString()
        };

        _userRegionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<UserRegion, bool>>>()))
            .Returns(new List<UserRegion> { userRegion }.AsQueryable());
        _userRegionRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<UserRegion>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRegion);

        _userContextServiceMock.Setup(c => c.GetCurrentUserId()).Returns(callerAuthUserId);

        var request = new UpdateUserRegionStatusByIdRequest(userRegionId, new UpdateUserRegionStatusDto
        {
            Status = UserRegionStatus.Active.ToString()
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("cannot approve or alter the status of your own region access");
        _userRegionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSuperAdminApprovesAnotherUserRegion_Succeeds()
    {
        // Arrange
        var userRegionId = Guid.NewGuid();
        var callerAuthUserId = "clerk_super_admin";
        var otherUserAuthId = "clerk_normal_user";

        var targetUser = new User
        {
            Id = Guid.NewGuid(),
            AuthUserId = otherUserAuthId,
            Email = "other@example.com",
            Role = new Role { Id = Guid.NewGuid(), Key = Roles.RegionalStaff, Name = "Regional Staff" }
        };

        var userRegion = new UserRegion
        {
            Id = userRegionId,
            UserId = targetUser.Id,
            User = targetUser,
            RegionId = Guid.NewGuid(),
            Region = new Region { Id = Guid.NewGuid(), Name = "NCR", Code = "NCR" },
            Status = UserRegionStatus.Pending.ToString()
        };

        _userRegionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<UserRegion, bool>>>()))
            .Returns(new List<UserRegion> { userRegion }.AsQueryable());
        _userRegionRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<UserRegion>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRegion);

        _userContextServiceMock.Setup(c => c.GetCurrentUserId()).Returns(callerAuthUserId);
        _userAuthorizationServiceMock.Setup(a => a.HasRoleAsync(Roles.SuperAdmin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateUserRegionStatusByIdRequest(userRegionId, new UpdateUserRegionStatusDto
        {
            Status = UserRegionStatus.Active.ToString()
        });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        userRegion.Status.Should().Be(UserRegionStatus.Active.ToString());
        _userRegionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
