using FluentAssertions;
using Moq;
using NTC.QoS.Application.Features.User.Handlers.Queries;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;
using System.Linq.Expressions;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Users;

public class GetUserByAuthUserIdHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserProvisioningService> _userProvisioningServiceMock;
    private readonly GetUserByAuthUserIdHandler _handler;

    public GetUserByAuthUserIdHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userProvisioningServiceMock = new Mock<IUserProvisioningService>();
        _handler = new GetUserByAuthUserIdHandler(_userRepositoryMock.Object, _userProvisioningServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUserWithoutProvisioning()
    {
        // Arrange
        var authUserId = "user_clerk_123";
        var user = new User
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(new List<User> { user }.AsQueryable());
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new GetUserByAuthUserIdRequest(authUserId), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data!.AuthUserId.Should().Be(authUserId);
        _userProvisioningServiceMock.Verify(s => s.EnsureUserExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotInDbAndAllowProvisioningIsTrue_TriggersProvisioning()
    {
        // Arrange
        var authUserId = "user_new_456";
        var provisionedUser = new User
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = "new@example.com",
            FirstName = "New",
            LastName = "User"
        };

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(new List<User>().AsQueryable());

        // First call: null; second call (after provisioning): provisionedUser
        _userRepositoryMock.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(provisionedUser);

        _userProvisioningServiceMock.Setup(s => s.EnsureUserExistsAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = true });

        // Act
        var result = await _handler.Handle(new GetUserByAuthUserIdRequest(authUserId, allowProvisioning: true), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data!.AuthUserId.Should().Be(authUserId);
        _userProvisioningServiceMock.Verify(s => s.EnsureUserExistsAsync(authUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotInDbAndAllowProvisioningIsFalse_DoesNotTriggerProvisioningAndReturnsNotFound()
    {
        // Arrange
        var authUserId = "user_nonexistent_789";

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(new List<User>().AsQueryable());
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(new GetUserByAuthUserIdRequest(authUserId, allowProvisioning: false), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("User not found.");
        _userProvisioningServiceMock.Verify(s => s.EnsureUserExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
