using FluentAssertions;
using Moq;
using NTC.QoS.Application.Features.User.Handlers.Commands;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Users;

public class CreateSessionHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IClerkUserService> _clerkUserServiceMock;
    private readonly CreateSessionHandler _handler;

    public CreateSessionHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _clerkUserServiceMock = new Mock<IClerkUserService>();
        _handler = new CreateSessionHandler(_userRepositoryMock.Object, _clerkUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsErrorResponse()
    {
        // Arrange
        var authUserId = "user_not_found";
        var request = new CreateSessionRequest { AuthUserId = authUserId };

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(new List<User>().AsQueryable());
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(authUserId);
        _clerkUserServiceMock.Verify(c => c.UpdateUserMetadataAsync(It.IsAny<string>(), It.IsAny<UserMetadataPayload>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserFoundWithRoleAndPermissions_UpdatesClerkMetadataAndReturnsSuccess()
    {
        // Arrange
        var authUserId = "user_valid_123";
        var request = new CreateSessionRequest { AuthUserId = authUserId };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Key = "admin",
            Name = "Admin",
            Permissions = ["read:reports", "write:reports"]
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = "admin@ntc.gov.ph",
            Role = role,
            RoleId = role.Id,
            IsDeleted = false
        };

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(new List<User> { user }.AsQueryable());
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        UserMetadataPayload? capturedPayload = null;
        _clerkUserServiceMock.Setup(c => c.UpdateUserMetadataAsync(authUserId, It.IsAny<UserMetadataPayload>()))
            .Callback<string, UserMetadataPayload>((id, payload) => capturedPayload = payload)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be("Session created successfully.");

        capturedPayload.Should().NotBeNull();
        var publicMeta = capturedPayload!.PublicMetadata as Dictionary<string, object>;
        publicMeta.Should().NotBeNull();
        publicMeta!["role"].Should().Be("admin");

        var permissions = publicMeta["permissions"] as List<string>;
        permissions.Should().NotBeNull();
        permissions.Should().BeEquivalentTo(new[] { "read:reports", "write:reports" });
    }

    [Fact]
    public async Task Handle_WhenUserFoundWithoutRoleOrPermissions_UpdatesClerkMetadataWithFallbacksAndReturnsSuccess()
    {
        // Arrange
        var authUserId = "user_minimal";
        var request = new CreateSessionRequest { AuthUserId = authUserId };

        var user = new User
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = "user@example.com",
            Role = null!,
            IsDeleted = false
        };

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(new List<User> { user }.AsQueryable());
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        UserMetadataPayload? capturedPayload = null;
        _clerkUserServiceMock.Setup(c => c.UpdateUserMetadataAsync(authUserId, It.IsAny<UserMetadataPayload>()))
            .Callback<string, UserMetadataPayload>((id, payload) => capturedPayload = payload)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeTrue();

        capturedPayload.Should().NotBeNull();
        var publicMeta = capturedPayload!.PublicMetadata as Dictionary<string, object>;
        publicMeta.Should().NotBeNull();
        publicMeta!["role"].Should().Be("unknown");
        publicMeta["permissions"].Should().BeEquivalentTo(new List<string>());
    }
}
