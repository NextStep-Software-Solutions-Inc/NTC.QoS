using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.User;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Implementation of user authorization service
/// </summary>
public class UserAuthorizationService : IUserAuthorizationService
{
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly IUserRegionRepository _userRegionRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserProvisioningService _userProvisioningService;
    private readonly ILogger<UserAuthorizationService> _logger;

    // Cache for user data to avoid repeated database calls within a request
    private UserDto? _cachedUser;
    private List<string>? _cachedPermissions;
    private List<Guid>? _cachedRegions;

    public UserAuthorizationService(
        IUserContextService userContextService,
        IUserRepository userRepository,
        IUserRegionRepository userRegionRepository,
        ApplicationDbContext dbContext,
        IUserProvisioningService userProvisioningService,
        ILogger<UserAuthorizationService> logger)
    {
        _userContextService = userContextService;
        _userRepository = userRepository;
        _userRegionRepository = userRegionRepository;
        _dbContext = dbContext;
        _userProvisioningService = userProvisioningService;
        _logger = logger;
    }

    public async Task<UserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedUser != null)
        {
            return _cachedUser;
        }

        var authUserId = _userContextService.GetCurrentUserId();
        if (string.IsNullOrEmpty(authUserId))
        {
            _logger.LogWarning("No authenticated user found in context");
            return null;
        }

        try
        {
            var userQuery = _userRepository.Query(u => u.AuthUserId == authUserId)
                .Include(u => u.Role);

            var user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);

            if (user == null)
            {
                _logger.LogInformation("User not found for AuthUserId: {AuthUserId}. Attempting JIT provisioning...", authUserId);
                var provisionResult = await _userProvisioningService.EnsureUserExistsAsync(authUserId, cancellationToken);
                if (provisionResult.Succeeded)
                {
                    user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);
                }
            }

            if (user == null)
            {
                _logger.LogWarning("User not found for AuthUserId: {AuthUserId}", authUserId);
                return null;
            }

            _cachedUser = new UserDto
            {
                Id = user.Id,
                AuthUserId = user.AuthUserId,
                Role = user.Role != null ? new()
                {
                    Id = user.Role.Id,
                    Name = user.Role.Name,
                    Key = user.Role.Key,
                    Description = user.Role.Description,
                    IsSystemRole = user.Role.IsSystemRole,
                    Scope = user.Role.Scope,
                    RegionId = user.Role.RegionId,
                    Permissions = user.Role.Permissions
                } : null,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            return _cachedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user for AuthUserId: {AuthUserId}", authUserId);
            return null;
        }
    }

    public async Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(permission))
        {
            return false;
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user?.Role != null && Roles.IsSuperAdmin(user.Role.Key))
        {
            return true;
        }

        var permissions = await GetUserPermissionsAsync(cancellationToken);
        return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> HasAnyPermissionAsync(IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        if (permissions == null || !permissions.Any())
        {
            return false;
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user?.Role != null && Roles.IsSuperAdmin(user.Role.Key))
        {
            return true;
        }

        var userPermissions = await GetUserPermissionsAsync(cancellationToken);
        return permissions.Any(p => userPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<bool> HasAllPermissionsAsync(IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        if (permissions == null || !permissions.Any())
        {
            return true; // No permissions required
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user?.Role != null && Roles.IsSuperAdmin(user.Role.Key))
        {
            return true;
        }

        var userPermissions = await GetUserPermissionsAsync(cancellationToken);
        return permissions.All(p => userPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<bool> HasRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(role))
        {
            return false;
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        return user?.Role?.Key?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
    }

    public async Task<List<Guid>> GetAccessibleRegionsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedRegions != null)
        {
            return _cachedRegions;
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user == null)
        {
            _cachedRegions = [];
            return _cachedRegions;
        }

        try
        {
            // Platform roles have implicit global access to all regions
            if (user.Role != null && Roles.IsPlatformRole(user.Role.Key))
            {
                _cachedRegions = await _dbContext.Regions
                    .Where(r => !r.IsDeleted)
                    .Select(r => r.Id)
                    .ToListAsync(cancellationToken);
                return _cachedRegions;
            }

            // Regional users only have access to explicitly assigned Active regions
            var userRegionsQuery = _userRegionRepository.Query(ur =>
                    ur.User.AuthUserId == user.AuthUserId &&
                    ur.Status == UserRegionStatus.Active.ToString() &&
                    !ur.IsDeleted)
                .Select(ur => ur.RegionId);

            _cachedRegions = await _userRegionRepository.ToListAsync(userRegionsQuery, cancellationToken);
            return _cachedRegions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accessible regions for user: {UserId}", user.Id);
            _cachedRegions = [];
            return _cachedRegions;
        }
    }

    public async Task<bool> HasRegionAccessAsync(Guid regionId, CancellationToken cancellationToken = default)
    {
        var accessibleRegions = await GetAccessibleRegionsAsync(cancellationToken);
        return accessibleRegions.Contains(regionId);
    }

    public async Task<List<string>> GetUserPermissionsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedPermissions != null)
        {
            return _cachedPermissions;
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user?.Role == null)
        {
            _cachedPermissions = [];
            return _cachedPermissions;
        }

        if (Roles.IsSuperAdmin(user.Role.Key))
        {
            _cachedPermissions = PermissionsRegistry.AllKeys.ToList();
            return _cachedPermissions;
        }

        _cachedPermissions = user.Role.Permissions ?? [];
        return _cachedPermissions;
    }

    public async Task<List<string>> GetUserPermissionsInRegionAsync(Guid regionId, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user?.Role == null)
        {
            return [];
        }

        if (Roles.IsSuperAdmin(user.Role.Key))
        {
            return PermissionsRegistry.AllKeys.ToList();
        }

        // Platform roles have implicit global access with their primary role permissions
        if (Roles.IsPlatformRole(user.Role.Key))
        {
            return user.Role.Permissions ?? [];
        }

        // User must have active access to the region
        var hasAccess = await HasRegionAccessAsync(regionId, cancellationToken);
        if (!hasAccess)
        {
            return [];
        }

        // Check if user has an assigned regional role with specific permissions
        var userRegionQuery = _userRegionRepository.Query(ur =>
                ur.User.AuthUserId == user.AuthUserId &&
                ur.RegionId == regionId &&
                ur.Status == UserRegionStatus.Active.ToString() &&
                !ur.IsDeleted)
            .Include(ur => ur.Role);

        var userRegion = await _userRegionRepository.FirstOrDefaultAsync(userRegionQuery, cancellationToken);
        if (userRegion?.Role?.Permissions is { Count: > 0 } regionalPermissions)
        {
            return regionalPermissions;
        }

        return user.Role.Permissions ?? [];
    }

    public async Task<bool> HasPermissionInRegionAsync(string permission, Guid regionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(permission))
        {
            return false;
        }

        var permissions = await GetUserPermissionsInRegionAsync(regionId, cancellationToken);
        return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}