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

        // Check if user already exists in DB by AuthUserId
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

        var clerkEmail = clerkUser.EmailAddresses?.FirstOrDefault()?.EmailAddress;

        // Check if an invited user exists with this email but without AuthUserId
        if (!string.IsNullOrEmpty(clerkEmail))
        {
            var emailUsers = await _mediator.Send(new GetAllUsersRequest(pageSize: 1, pageNumber: 1, email: clerkEmail), cancellationToken);
            var invitedUser = emailUsers?.Data?.FirstOrDefault();
            if (invitedUser != null && (string.IsNullOrEmpty(invitedUser.AuthUserId) || invitedUser.AuthUserId == clerkUser.Id))
            {
                _logger.LogInformation("Found invited user {UserId} with email {Email}. Linking AuthUserId {AuthUserId}...", invitedUser.Id, clerkEmail, authUserId);
                var linkRequest = new UpdateUserByIdRequest(invitedUser.Id, new UpdateUserDto
                {
                    AuthUserId = clerkUser.Id,
                    FirstName = string.IsNullOrEmpty(invitedUser.FirstName) ? clerkUser.FirstName : invitedUser.FirstName,
                    LastName = string.IsNullOrEmpty(invitedUser.LastName) ? clerkUser.LastName : invitedUser.LastName,
                    Email = clerkEmail
                });

                var linkResponse = await _mediator.Send(linkRequest, cancellationToken);
                if (linkResponse.Succeeded)
                {
                    try
                    {
                        await _mediator.Send(new CreateSessionRequest { AuthUserId = authUserId }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update Clerk metadata during invited user linking for {AuthUserId}", authUserId);
                    }
                    return linkResponse;
                }
            }
        }

        var (metaRoleId, metaRegions) = ParseMetadata(clerkUser.PublicMetadata);

        var createUserRequest = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = clerkUser.Id,
            Email = clerkEmail ?? "",
            FirstName = clerkUser.FirstName ?? "",
            LastName = clerkUser.LastName ?? "",
            RoleId = metaRoleId,
            Regions = metaRegions.Count > 0 ? metaRegions : null
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

        var webhookEmail = userData.EmailAddresses?.FirstOrDefault()?.EmailAddress;
        if (!string.IsNullOrEmpty(webhookEmail))
        {
            var emailUsers = await _mediator.Send(new GetAllUsersRequest(pageSize: 1, pageNumber: 1, email: webhookEmail), cancellationToken);
            var invitedUser = emailUsers?.Data?.FirstOrDefault();
            if (invitedUser != null && (string.IsNullOrEmpty(invitedUser.AuthUserId) || invitedUser.AuthUserId == userData.Id))
            {
                var updateRequest = new UpdateUserByIdRequest(invitedUser.Id, new UpdateUserDto
                {
                    AuthUserId = userData.Id,
                    FirstName = userData.FirstName ?? invitedUser.FirstName ?? "",
                    LastName = userData.LastName ?? invitedUser.LastName ?? "",
                    Email = webhookEmail
                });

                return await _mediator.Send(updateRequest, cancellationToken);
            }
        }

        var (metaRoleId, metaRegions) = ParseMetadata(userData.PublicMetadata);

        var createRequest = new CreateUserRequest(new CreateUserDto
        {
            AuthUserId = userData.Id,
            Email = webhookEmail ?? "",
            FirstName = userData.FirstName ?? "",
            LastName = userData.LastName ?? "",
            RoleId = metaRoleId,
            Regions = metaRegions.Count > 0 ? metaRegions : null
        });

        return await _mediator.Send(createRequest, cancellationToken);
    }

    private static (Guid RoleId, List<CreateUserRegionAssignmentDto> Regions) ParseMetadata(System.Text.Json.JsonElement? publicMetadata)
    {
        Guid roleId = Guid.Empty;
        var regions = new List<CreateUserRegionAssignmentDto>();

        if (publicMetadata.HasValue && publicMetadata.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var meta = publicMetadata.Value;
            if (meta.TryGetProperty("role_id", out var roleProp) && Guid.TryParse(roleProp.GetString(), out var parsedRoleId))
            {
                roleId = parsedRoleId;
            }

            if (meta.TryGetProperty("regions", out var regionsProp) && regionsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var rElem in regionsProp.EnumerateArray())
                {
                    if (rElem.TryGetProperty("region_id", out var regIdProp) && Guid.TryParse(regIdProp.GetString(), out var regId))
                    {
                        Guid? regionalRoleId = null;
                        if (rElem.TryGetProperty("role_id", out var rRoleProp) && Guid.TryParse(rRoleProp.GetString(), out var parsedRRole))
                        {
                            regionalRoleId = parsedRRole;
                        }

                        var status = rElem.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "Active";
                        regions.Add(new CreateUserRegionAssignmentDto
                        {
                            RegionId = regId,
                            RoleId = regionalRoleId,
                            Status = string.IsNullOrWhiteSpace(status) ? "Active" : status
                        });
                    }
                }
            }
        }

        return (roleId, regions);
    }
}
