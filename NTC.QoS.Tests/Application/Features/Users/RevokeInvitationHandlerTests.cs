using NTC.QoS.Domain.Authorization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Application.Features.User.Handlers.Commands;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Users;

public class RevokeInvitationHandlerTests
{
    private readonly Mock<IClerkUserService> _clerkUserServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserRegionRepository> _userRegionRepositoryMock;
    private readonly Mock<ILogger<RevokeInvitationHandler>> _loggerMock;
    private readonly RevokeInvitationHandler _handler;

    public RevokeInvitationHandlerTests()
    {
        _clerkUserServiceMock = new Mock<IClerkUserService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _userRegionRepositoryMock = new Mock<IUserRegionRepository>();
        _loggerMock = new Mock<ILogger<RevokeInvitationHandler>>();

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<global::NTC.QoS.Domain.Entities.User, bool>>>()))
            .Returns(new List<global::NTC.QoS.Domain.Entities.User>().AsQueryable());
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<global::NTC.QoS.Domain.Entities.User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((global::NTC.QoS.Domain.Entities.User?)null);

        _handler = new RevokeInvitationHandler(
            _clerkUserServiceMock.Object,
            _userRepositoryMock.Object,
            _userRegionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidId_RevokesInvitationSuccessfully()
    {
        // Arrange
        var invitationId = "inv_revoke_test";
        _clerkUserServiceMock.Setup(c => c.RevokeInvitationAsync(invitationId))
            .ReturnsAsync(new ClerkInvitationResponse
            {
                Id = invitationId,
                EmailAddress = "revoke_test@example.com",
                Status = "revoked"
            });

        // Act
        var result = await _handler.Handle(new RevokeInvitationRequest(invitationId), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Contain("revoke_test@example.com revoked successfully");
        _clerkUserServiceMock.Verify(c => c.RevokeInvitationAsync(invitationId), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmptyId_ReturnsValidationError()
    {
        // Act
        var result = await _handler.Handle(new RevokeInvitationRequest("   "), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Invitation ID is required");
        _clerkUserServiceMock.Verify(c => c.RevokeInvitationAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenClerkApiThrows_ReturnsFailure()
    {
        // Arrange
        var invitationId = "inv_not_found";
        _clerkUserServiceMock.Setup(c => c.RevokeInvitationAsync(invitationId))
            .ThrowsAsync(new InvalidOperationException("Invitation not found or already revoked."));

        // Act
        var result = await _handler.Handle(new RevokeInvitationRequest(invitationId), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Message.Should().Contain("Invitation not found or already revoked.");
    }

    [Fact]
    public async Task Handle_WhenInvitationDoesNotExistInClerk_ReturnsFailureWithErrorDetails()
    {
        // Arrange
        var nonExistentInvitationId = "inv_non_existent_999";
        _clerkUserServiceMock.Setup(c => c.RevokeInvitationAsync(nonExistentInvitationId))
            .ThrowsAsync(new InvalidOperationException("Resource not found: invitation inv_non_existent_999 does not exist."));

        // Act
        var result = await _handler.Handle(new RevokeInvitationRequest(nonExistentInvitationId), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Message.Should().Contain("Resource not found");
        result.Errors.Should().ContainKey("Revoke");
        result.Errors!["Revoke"].Should().ContainSingle(e => e.Contains("inv_non_existent_999"));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(nonExistentInvitationId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvitationAlreadyAccepted_ReturnsFailureWithErrorDetails()
    {
        // Arrange
        var acceptedInvitationId = "inv_already_accepted";
        _clerkUserServiceMock.Setup(c => c.RevokeInvitationAsync(acceptedInvitationId))
            .ThrowsAsync(new InvalidOperationException("Invitation has already been accepted and cannot be revoked."));

        // Act
        var result = await _handler.Handle(new RevokeInvitationRequest(acceptedInvitationId), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Message.Should().Contain("already been accepted");
        result.Errors.Should().ContainKey("Revoke");
    }

    [Fact]
    public async Task Handle_WhenRevoking_CleansUpUnonboardedUserFromDatabase()
    {
        // Arrange
        var invitationId = "inv_clean_test";
        var email = "clean_test@example.com";

        _clerkUserServiceMock.Setup(c => c.RevokeInvitationAsync(invitationId))
            .ReturnsAsync(new ClerkInvitationResponse
            {
                Id = invitationId,
                EmailAddress = email,
                Status = "revoked"
            });

        var userRepositoryMock = new Mock<IUserRepository>();
        var userRegionRepositoryMock = new Mock<IUserRegionRepository>();

        var unonboardedUser = new global::NTC.QoS.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = email,
            AuthUserId = "user_unonboarded",
            Role = new global::NTC.QoS.Domain.Entities.Role { Key = Roles.User, Name = "User" }
        };

        userRepositoryMock.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<global::NTC.QoS.Domain.Entities.User, bool>>>()))
            .Returns(new List<global::NTC.QoS.Domain.Entities.User> { unonboardedUser }.AsQueryable());
        userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<global::NTC.QoS.Domain.Entities.User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(unonboardedUser);

        userRegionRepositoryMock.Setup(r => r.CountAsync(It.IsAny<IQueryable<global::NTC.QoS.Domain.Aggregates.UserRegion>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handlerWithRepos = new RevokeInvitationHandler(
            _clerkUserServiceMock.Object,
            userRepositoryMock.Object,
            userRegionRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await handlerWithRepos.Handle(new RevokeInvitationRequest(invitationId), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        userRepositoryMock.Verify(r => r.Delete(unonboardedUser), Times.Once);
        userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
