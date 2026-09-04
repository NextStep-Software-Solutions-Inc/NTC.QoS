using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Handles user provisioning, syncing, and webhook upserting between Clerk and the local database.
/// </summary>
public class UserProvisioningService(
    IMediator mediator,
    IClerkUserService clerkUserService,
    ILogger<UserProvisioningService> logger) : IUserProvisioningService
{
    private readonly IMediator _mediator = mediator;
    private readonly IClerkUserService _clerkUserService = clerkUserService;
    private readonly ILogger<UserProvisioningService> _logger = logger;

    public async Task<ApiResponse<UserDto>> EnsureUserExistsAsync(string authUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authUserId))
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid AuthUserId provided."
            };
        }

        // Check if user already exists in DB
        var userQuery = new GetUserByAuthUserIdRequest(authUserId);
        var userResponse = await _mediator.Send(userQuery, cancellationToken);
        if (userResponse.Succeeded && userResponse.Data != null)
        {
            return userResponse;
        }

        _logger.LogInformation("User with AuthUserId {AuthUserId} not found in database. Fetching from Clerk...", authUserId);

        // Fetch user from Clerk
        var clerkUser = await _clerkUserService.GetUserByIdAsync(authUserId);
        if (clerkUser == null)
        {
            _logger.LogWarning("User with AuthUserId {AuthUserId} not found in Clerk.", authUserId);
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = $"User with AuthUserId '{authUserId}' not found in Clerk."
            };
        }

        var createUserRequest = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = clerkUser.Id,
            Email = clerkUser.EmailAddresses?.FirstOrDefault()?.EmailAddress ?? "",
            FirstName = clerkUser.FirstName ?? "",
            LastName = clerkUser.LastName ?? "",
            RoleId = Guid.Empty,
            OrganizationId = Guid.Empty
        });

        var createResponse = await _mediator.Send(createUserRequest, cancellationToken);
        if (!createResponse.Succeeded)
        {
            _logger.LogError("Failed to create user for AuthUserId {AuthUserId}: {Message}", authUserId, createResponse.Message);
            return createResponse;
        }

        _logger.LogInformation("Successfully provisioned user for AuthUserId {AuthUserId}. Syncing session metadata...", authUserId);

        // Sync metadata to Clerk
        try
        {
            await _mediator.Send(new CreateSessionRequest { AuthUserId = authUserId }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Clerk metadata during provisioning for {AuthUserId}", authUserId);
        }

        return createResponse;
    }

    public async Task<ApiResponse<UserDto>> UpsertUserFromWebhookAsync(ClerkUserData userData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userData.Id))
        {
            return new ApiResponse<UserDto>
            {
                Succeeded = false,
                Message = "Invalid user data in webhook payload: missing user ID."
            };
        }

        var userQuery = new GetUserByAuthUserIdRequest(userData.Id);
        var userResponse = await _mediator.Send(userQuery, cancellationToken);
        if (userResponse.Succeeded && userResponse.Data != null)
        {
            var updateRequest = new UpdateUserByIdRequest(userResponse.Data.Id, new UpdateUserDto
            {
                AuthUserId = userData.Id,
                FirstName = userData.FirstName ?? "",
                LastName = userData.LastName ?? "",
                Email = userData.EmailAddresses?.FirstOrDefault()?.EmailAddress ?? ""
            });

            return await _mediator.Send(updateRequest, cancellationToken);
        }

        var createRequest = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = userData.Id,
            Email = userData.EmailAddresses?.FirstOrDefault()?.EmailAddress ?? "",
            FirstName = userData.FirstName ?? "",
            LastName = userData.LastName ?? "",
            RoleId = Guid.Empty,
            OrganizationId = Guid.Empty
        });

        return await _mediator.Send(createRequest, cancellationToken);
    }
}
