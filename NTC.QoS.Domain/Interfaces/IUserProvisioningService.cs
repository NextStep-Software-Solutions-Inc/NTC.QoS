using NTC.QoS.Domain.Models;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service responsible for provisioning and syncing users between external identity providers (e.g. Clerk) and the local database.
/// </summary>
public interface IUserProvisioningService
{
    /// <summary>
    /// Ensures that a user with the specified AuthUserId exists in the local database.
    /// If not found locally, fetches their profile from the identity provider, creates them with default roles, and syncs session metadata.
    /// </summary>
    Task<ApiResponse<UserDto>> EnsureUserExistsAsync(string authUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a user from a webhook user payload.
    /// </summary>
    Task<ApiResponse<UserDto>> UpsertUserFromWebhookAsync(ClerkUserData userData, CancellationToken cancellationToken = default);
}
