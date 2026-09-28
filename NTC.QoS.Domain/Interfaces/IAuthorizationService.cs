using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service for user authorization and permission operations
/// </summary>
public interface IUserAuthorizationService
{
    /// <summary>
    /// Gets the current user's information including roles and permissions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user information or null if not found</returns>
    Task<UserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has the specified permission
    /// </summary>
    /// <param name="permission">Permission key to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has permission, false otherwise</returns>
    Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has any of the specified permissions
    /// </summary>
    /// <param name="permissions">List of permission keys to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has any of the permissions, false otherwise</returns>
    Task<bool> HasAnyPermissionAsync(IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has all of the specified permissions
    /// </summary>
    /// <param name="permissions">List of permission keys to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has all permissions, false otherwise</returns>
    Task<bool> HasAllPermissionsAsync(IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has the specified role
    /// </summary>
    /// <param name="role">Role key to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has role, false otherwise</returns>
    Task<bool> HasRoleAsync(string role, CancellationToken cancellationToken = default);

    /// <summary>

    /// <summary>
    /// Gets the regions accessible by the current user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of accessible region IDs</returns>
    Task<List<Guid>> GetAccessibleRegionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has access to the specified region
    /// </summary>
    /// <param name="regionId">Region ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has access to region, false otherwise</returns>
    Task<bool> HasRegionAccessAsync(Guid regionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user's permissions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of permission keys</returns>
    Task<List<string>> GetUserPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user's effective permissions within a specific region.
    /// Evaluates regional role assignments if assigned, falling back to primary role permissions.
    /// </summary>
    /// <param name="regionId">Region ID to evaluate permissions for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of permission keys valid in this region</returns>
    Task<List<string>> GetUserPermissionsInRegionAsync(Guid regionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has the specified permission within a specific region.
    /// </summary>
    /// <param name="permission">Permission key to check</param>
    /// <param name="regionId">Region ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has permission in the region, false otherwise</returns>
    Task<bool> HasPermissionInRegionAsync(string permission, Guid regionId, CancellationToken cancellationToken = default);
}