using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Application.Features.UserRegion.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Handles user provisioning, syncing, and webhook upserting between Clerk and the local database.
/// </summary>
public class UserProvisioningService : IUserProvisioningService
{
    private readonly IMediator _mediator;
    private readonly IClerkUserService _clerkUserService;
    private readonly IUserRepository? _userRepository;
    private readonly IUserRegionRepository? _userRegionRepository;
    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(
        IMediator mediator,
        IClerkUserService clerkUserService,
        ILogger<UserProvisioningService> logger,
        IUserRepository? userRepository = null,
        IUserRegionRepository? userRegionRepository = null)
    {
        _mediator = mediator;
        _clerkUserService = clerkUserService;
        _logger = logger;
        _userRepository = userRepository;
        _userRegionRepository = userRegionRepository;
    }

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
            var existingUser = userResponse.Data;
            await ReconcileUserInvitationAsync(existingUser, cancellationToken);
            return await _mediator.Send(userQuery, cancellationToken);
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

        // Fallback: When a user accepts a Clerk invitation, Clerk does not always copy invitation metadata to the user profile.
        // Look up the invitation directly by email to recover the admin-assigned role and regions.
        if ((metaRoleId == Guid.Empty || metaRegions.Count == 0) && !string.IsNullOrEmpty(clerkEmail))
        {
            try
            {
                var invitations = await _clerkUserService.GetInvitationsAsync();
                var invite = invitations
                    .Where(i => string.Equals(i.EmailAddress, clerkEmail, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt ?? 0)
                    .FirstOrDefault();

                if (invite?.PublicMetadata.HasValue == true)
                {
                    var (inviteRoleId, inviteRegions) = ParseMetadata(invite.PublicMetadata);
                    if (metaRoleId == Guid.Empty && inviteRoleId != Guid.Empty)
                    {
                        metaRoleId = inviteRoleId;
                    }
                    if (metaRegions.Count == 0 && inviteRegions.Count > 0)
                    {
                        metaRegions = inviteRegions;
                    }

                    _logger.LogInformation("Recovered role {RoleId} and {RegionCount} regions from Clerk invitation for {Email}",
                        metaRoleId, metaRegions.Count, clerkEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve invitation fallback metadata for email {Email}", clerkEmail);
            }
        }

        // Sync recovered metadata to Clerk user profile
        if (metaRoleId != Guid.Empty || metaRegions.Count > 0)
        {
            try
            {
                await _clerkUserService.UpdateUserMetadataAsync(clerkUser.Id, new UserMetadataPayload
                {
                    PublicMetadata = new
                    {
                        role_id = metaRoleId != Guid.Empty ? metaRoleId.ToString() : null,
                        regions = metaRegions.Select(r => new
                        {
                            region_id = r.RegionId.ToString(),
                            role_id = r.RoleId?.ToString(),
                            status = r.Status
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync recovered invitation metadata to Clerk user {AuthUserId}", clerkUser.Id);
            }
        }

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

        if ((metaRoleId == Guid.Empty || metaRegions.Count == 0) && !string.IsNullOrEmpty(webhookEmail))
        {
            try
            {
                var invitations = await _clerkUserService.GetInvitationsAsync();
                var invite = invitations
                    .Where(i => string.Equals(i.EmailAddress, webhookEmail, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt ?? 0)
                    .FirstOrDefault();

                if (invite?.PublicMetadata.HasValue == true)
                {
                    var (inviteRoleId, inviteRegions) = ParseMetadata(invite.PublicMetadata);
                    if (metaRoleId == Guid.Empty && inviteRoleId != Guid.Empty)
                    {
                        metaRoleId = inviteRoleId;
                    }
                    if (metaRegions.Count == 0 && inviteRegions.Count > 0)
                    {
                        metaRegions = inviteRegions;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve invitation fallback metadata for webhook email {Email}", webhookEmail);
            }

            // Sync recovered metadata to Clerk user profile so subsequent sessions/tokens have the role and regions
            if (metaRoleId != Guid.Empty || metaRegions.Count > 0)
            {
                try
                {
                    await _clerkUserService.UpdateUserMetadataAsync(userData.Id, new UserMetadataPayload
                    {
                        PublicMetadata = new
                        {
                            role_id = metaRoleId != Guid.Empty ? metaRoleId.ToString() : null,
                            regions = metaRegions.Select(r => new
                            {
                                region_id = r.RegionId.ToString(),
                                role_id = r.RoleId?.ToString(),
                                status = r.Status
                            }).ToList()
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync recovered invitation metadata to Clerk user {AuthUserId}", userData.Id);
                }
            }
        }

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

    private async Task ReconcileUserInvitationAsync(UserDto existingUser, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(existingUser.Email))
        {
            return;
        }

        var isDefaultUserRole = existingUser.Role == null || string.Equals(existingUser.Role.Key, Domain.Authorization.Roles.User, StringComparison.OrdinalIgnoreCase);

        // Check if existing user has any assigned regions
        var hasRegions = false;
        if (_userRegionRepository != null)
        {
            var regionQuery = _userRegionRepository.Query(ur => ur.UserId == existingUser.Id && !ur.IsDeleted);
            hasRegions = await _userRegionRepository.CountAsync(regionQuery, cancellationToken) > 0;
        }
        else
        {
            var regionsResponse = await _mediator.Send(new GetAllUserRegionsRequest
            {
                AuthUserIds = [existingUser.AuthUserId],
                PageSize = 10
            }, cancellationToken);
            hasRegions = regionsResponse?.Data != null && regionsResponse.Data.Count > 0;
        }

        // If user already has assigned regions and does not have the default role, no repair needed
        if (hasRegions && !isDefaultUserRole)
        {
            return;
        }

        try
        {
            var invitations = await _clerkUserService.GetInvitationsAsync();
            var invite = invitations
                .Where(i => string.Equals(i.EmailAddress, existingUser.Email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt ?? 0)
                .FirstOrDefault();

            if (invite?.PublicMetadata.HasValue == true)
            {
                var (inviteRoleId, inviteRegions) = ParseMetadata(invite.PublicMetadata);
                var changed = false;

                if (!hasRegions && inviteRegions.Count > 0 && _userRegionRepository != null)
                {
                    _logger.LogInformation("Assigning {Count} missing invited regions to existing user {UserId} ({Email})",
                        inviteRegions.Count, existingUser.Id, existingUser.Email);

                    var newRegions = inviteRegions
                        .GroupBy(r => r.RegionId)
                        .Select(g => g.First())
                        .Select(r => new Domain.Aggregates.UserRegion
                        {
                            Id = Guid.NewGuid(),
                            UserId = existingUser.Id,
                            RegionId = r.RegionId,
                            RoleId = r.RoleId ?? (inviteRoleId != Guid.Empty ? inviteRoleId : (existingUser.Role?.Id ?? Guid.Empty)),
                            Status = string.IsNullOrWhiteSpace(r.Status) ? Domain.Enums.UserRegionStatus.Active.ToString() : r.Status
                        }).ToList();

                    _userRegionRepository.AddRange(newRegions);
                    await _userRegionRepository.SaveChangesAsync(cancellationToken);
                    changed = true;
                }

                if (isDefaultUserRole && inviteRoleId != Guid.Empty && _userRepository != null)
                {
                    _logger.LogInformation("Updating role for existing invited user {UserId} ({Email}) to {RoleId}",
                        existingUser.Id, existingUser.Email, inviteRoleId);

                    var dbUser = await _userRepository.FirstOrDefaultAsync(
                        _userRepository.Query(u => u.Id == existingUser.Id), cancellationToken);

                    if (dbUser != null)
                    {
                        dbUser.RoleId = inviteRoleId;
                        _userRepository.Update(dbUser);
                        await _userRepository.SaveChangesAsync(cancellationToken);
                        changed = true;
                    }
                }

                if (changed)
                {
                    try
                    {
                        await _mediator.Send(new CreateSessionRequest { AuthUserId = existingUser.AuthUserId }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update Clerk session metadata after reconciling user {AuthUserId}", existingUser.AuthUserId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reconcile invitation metadata for existing user {AuthUserId}", existingUser.AuthUserId);
        }
    }

    private static (Guid RoleId, List<CreateUserRegionAssignmentDto> Regions) ParseMetadata(System.Text.Json.JsonElement? publicMetadata)
    {
        Guid roleId = Guid.Empty;
        var regions = new List<CreateUserRegionAssignmentDto>();

        if (!publicMetadata.HasValue)
        {
            return (roleId, regions);
        }

        System.Text.Json.JsonDocument? parsedDoc = null;
        try
        {
            System.Text.Json.JsonElement meta;
            if (publicMetadata.Value.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var jsonStr = publicMetadata.Value.GetString();
                if (string.IsNullOrWhiteSpace(jsonStr))
                {
                    return (roleId, regions);
                }
                parsedDoc = System.Text.Json.JsonDocument.Parse(jsonStr);
                meta = parsedDoc.RootElement;
            }
            else if (publicMetadata.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                meta = publicMetadata.Value;
            }
            else
            {
                return (roleId, regions);
            }

            // Safely parse role_id (supports string, raw text, and legacy 'role' key)
            if (meta.TryGetProperty("role_id", out var roleProp) || meta.TryGetProperty("role", out roleProp))
            {
                var roleStr = roleProp.ValueKind == System.Text.Json.JsonValueKind.String
                    ? roleProp.GetString()
                    : roleProp.ValueKind == System.Text.Json.JsonValueKind.Number ? roleProp.GetRawText() : null;

                if (Guid.TryParse(roleStr, out var parsedRoleId))
                {
                    roleId = parsedRoleId;
                }
            }

            // Safely parse regions array (supports both object array and legacy ID string array)
            if (meta.TryGetProperty("regions", out var regionsProp) && regionsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var rElem in regionsProp.EnumerateArray())
                {
                    // Case 1: Structured object with region_id (or id), role_id, status
                    if (rElem.ValueKind == System.Text.Json.JsonValueKind.Object &&
                        (rElem.TryGetProperty("region_id", out var regIdProp) || rElem.TryGetProperty("id", out regIdProp)))
                    {
                        var regIdStr = regIdProp.ValueKind == System.Text.Json.JsonValueKind.String ? regIdProp.GetString() : null;
                        if (Guid.TryParse(regIdStr, out var regId))
                        {
                            Guid? regionalRoleId = null;
                            if (rElem.TryGetProperty("role_id", out var rRoleProp) &&
                                rRoleProp.ValueKind == System.Text.Json.JsonValueKind.String &&
                                Guid.TryParse(rRoleProp.GetString(), out var parsedRRole))
                            {
                                regionalRoleId = parsedRRole;
                            }

                            string status = "Active";
                            if (rElem.TryGetProperty("status", out var statusProp) &&
                                statusProp.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                status = statusProp.GetString() ?? "Active";
                            }

                            regions.Add(new CreateUserRegionAssignmentDto
                            {
                                RegionId = regId,
                                RoleId = regionalRoleId,
                                Status = string.IsNullOrWhiteSpace(status) ? "Active" : status
                            });
                        }
                    }
                    // Case 2: Legacy array of raw region ID strings or codes
                    else if (rElem.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var rawStr = rElem.GetString();
                        if (Guid.TryParse(rawStr, out var rawGuid))
                        {
                            regions.Add(new CreateUserRegionAssignmentDto
                            {
                                RegionId = rawGuid,
                                RoleId = null,
                                Status = "Active"
                            });
                        }
                    }
                }
            }
        }
        catch
        {
            // Non-fatal: if metadata payload structure drifted, return whatever was parsed safely
        }
        finally
        {
            parsedDoc?.Dispose();
        }

        return (roleId, regions);
    }
}
