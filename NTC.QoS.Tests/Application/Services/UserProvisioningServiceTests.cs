using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;
using Xunit;

namespace NTC.QoS.Tests.Application.Services;

public class UserProvisioningServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IClerkUserService> _clerkUserServiceMock;
    private readonly Mock<ILogger<UserProvisioningService>> _loggerMock;
    private readonly UserProvisioningService _service;

    public UserProvisioningServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _clerkUserServiceMock = new Mock<IClerkUserService>();
        _loggerMock = new Mock<ILogger<UserProvisioningService>>();
        _service = new UserProvisioningService(_mediatorMock.Object, _clerkUserServiceMock.Object, _loggerMock.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task EnsureUserExistsAsync_WhenAuthUserIdInvalid_ReturnsError(string? authUserId)
    {
        // Act
        var result = await _service.EnsureUserExistsAsync(authUserId!);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Invalid AuthUserId");
    }

    [Fact]
    public async Task EnsureUserExistsAsync_WhenUserAlreadyExistsInDb_ReturnsExistingUserWithoutCallingClerk()
    {
        // Arrange
        var authUserId = "user_existing_123";
        var existingUserDto = new UserDto
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = "existing@example.com",
            Role = null,
            FirstName = "Existing",
            LastName = "User"
        };

        _mediatorMock.Setup(m => m.Send(It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == authUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = true,
                Data = existingUserDto
            });

        // Act
        var result = await _service.EnsureUserExistsAsync(authUserId);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(existingUserDto);
        _clerkUserServiceMock.Verify(c => c.GetUserByIdAsync(It.IsAny<string>()), Times.Never);
        _mediatorMock.Verify(m => m.Send(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureUserExistsAsync_WhenUserNotInDbAndFoundInClerk_CreatesUserSyncsSessionAndReturnsSuccess()
    {
        // Arrange
        var authUserId = "user_new_456";
        _mediatorMock.Setup(m => m.Send(It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == authUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "User not found."
            });

        var clerkUserResponse = new ClerkUserResponse
        {
            Id = authUserId,
            FirstName = "New",
            LastName = "User",
            EmailAddresses = [new() { Id = "email_1", EmailAddress = "new@example.com" }]
        };
        _clerkUserServiceMock.Setup(c => c.GetUserByIdAsync(authUserId))
            .ReturnsAsync(clerkUserResponse);

        var createdUserDto = new UserDto
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = "new@example.com",
            Role = null,
            FirstName = "New",
            LastName = "User"
        };

        _mediatorMock.Setup(m => m.Send(It.Is<CreateUserRequest>(r => r.User.AuthUserId == authUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = true,
                Data = createdUserDto
            });

        _mediatorMock.Setup(m => m.Send(It.Is<CreateSessionRequest>(r => r.AuthUserId == authUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<bool>
            {
                Succeeded = true,
                Data = true
            });

        // Act
        var result = await _service.EnsureUserExistsAsync(authUserId);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(createdUserDto);
        _clerkUserServiceMock.Verify(c => c.GetUserByIdAsync(authUserId), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.Is<CreateUserRequest>(r => r.User.AuthUserId == authUserId), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.Is<CreateSessionRequest>(r => r.AuthUserId == authUserId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureUserExistsAsync_WhenClerkUserHasNoMetadata_RecoversRoleAndRegionsFromInvitation()
    {
        // Arrange
        var authUserId = "user_invited_789";
        var userEmail = "invited@example.com";
        var roleId = Guid.NewGuid();
        var regionId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == authUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "User not found."
            });

        var clerkUserResponse = new ClerkUserResponse
        {
            Id = authUserId,
            FirstName = "Invited",
            LastName = "User",
            EmailAddresses = [new() { Id = "email_inv", EmailAddress = userEmail }],
            PublicMetadata = null
        };
        _clerkUserServiceMock.Setup(c => c.GetUserByIdAsync(authUserId))
            .ReturnsAsync(clerkUserResponse);

        var inviteJson = System.Text.Json.JsonDocument.Parse($"{{\"role_id\": \"{roleId}\", \"regions\": [{{\"region_id\": \"{regionId}\", \"status\": \"Active\"}}]}}").RootElement;
        var invitations = new List<ClerkInvitationResponse>
        {
            new()
            {
                Id = "inv_123",
                EmailAddress = userEmail,
                Status = "accepted",
                PublicMetadata = inviteJson
            }
        };
        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync(null))
            .ReturnsAsync(invitations);

        var createdUserDto = new UserDto
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = userEmail,
            Role = null,
            FirstName = "Invited",
            LastName = "User"
        };

        _mediatorMock.Setup(m => m.Send(It.Is<CreateUserRequest>(r =>
                r.User.AuthUserId == authUserId &&
                r.User.RoleId == roleId &&
                r.User.Regions != null &&
                r.User.Regions.Any(reg => reg.RegionId == regionId)),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = true,
                Data = createdUserDto
            });

        _mediatorMock.Setup(m => m.Send(It.Is<CreateSessionRequest>(r => r.AuthUserId == authUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<bool>
            {
                Succeeded = true,
                Data = true
            });

        // Act
        var result = await _service.EnsureUserExistsAsync(authUserId);

        // Assert
        result.Succeeded.Should().BeTrue();
        _clerkUserServiceMock.Verify(c => c.GetInvitationsAsync(null), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.Is<CreateUserRequest>(r =>
            r.User.RoleId == roleId &&
            r.User.Regions!.Count == 1 &&
            r.User.Regions[0].RegionId == regionId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureUserExistsAsync_WhenUserNotFoundInClerk_ReturnsNotFound()
    {
        // Arrange
        var authUserId = "user_ghost";
        _mediatorMock.Setup(m => m.Send(It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == authUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "User not found."
            });

        _clerkUserServiceMock.Setup(c => c.GetUserByIdAsync(authUserId))
            .ReturnsAsync((ClerkUserResponse?)null);

        // Act
        var result = await _service.EnsureUserExistsAsync(authUserId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("not found in Clerk");
        _mediatorMock.Verify(m => m.Send(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpsertUserFromWebhookAsync_WhenUserExistsInDb_UpdatesUser()
    {
        // Arrange
        var authUserId = "user_update_789";
        var existingUserId = Guid.NewGuid();
        var userData = new ClerkUserData
        {
            Id = authUserId,
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast",
            EmailAddresses = [new() { EmailAddress = "updated@example.com" }]
        };

        _mediatorMock.Setup(m => m.Send(It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == authUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = true,
                Data = new UserDto
                {
                    Id = existingUserId,
                    AuthUserId = authUserId,
                    Email = "updated@example.com",
                    Role = null
                }
            });

        _mediatorMock.Setup(m => m.Send(It.Is<UpdateUserByIdRequest>(r => r.Id == existingUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = true,
                Message = "User updated successfully."
            });

        // Act
        var result = await _service.UpsertUserFromWebhookAsync(userData);

        // Assert
        result.Succeeded.Should().BeTrue();
        _mediatorMock.Verify(m => m.Send(It.Is<UpdateUserByIdRequest>(r => r.Id == existingUserId && r.User.FirstName == "UpdatedFirst"), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpsertUserFromWebhookAsync_WhenUserNotInDb_CreatesUser()
    {
        // Arrange
        var authUserId = "user_create_999";
        var userData = new ClerkUserData
        {
            Id = authUserId,
            FirstName = "BrandNew",
            LastName = "User",
            EmailAddresses = [new() { EmailAddress = "brandnew@example.com" }]
        };

        _mediatorMock.Setup(m => m.Send(It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == authUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "User not found."
            });

        _mediatorMock.Setup(m => m.Send(It.Is<CreateUserRequest>(r => r.User.AuthUserId == authUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = true,
                Message = "User created successfully."
            });

        // Act
        var result = await _service.UpsertUserFromWebhookAsync(userData);

        // Assert
        result.Succeeded.Should().BeTrue();
        _mediatorMock.Verify(m => m.Send(It.Is<CreateUserRequest>(r => r.User.AuthUserId == authUserId && r.User.FirstName == "BrandNew"), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserByIdRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
