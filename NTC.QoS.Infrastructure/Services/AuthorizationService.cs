using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        ILogger<UserAuthorizationService> logger)
    {
        _userContextService = userContextService;
        _userRepository = userRepository;
        _userRegionRepository = userRegionRepository;
        _dbContext = dbContext;
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
                .Include(u => u.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .Include(u => u.Organization);

            var user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);

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
                    Description = user.Role.Description
                } : null,
                Organization = user.Organization != null ? new()
                {
                    Id = user.Organization.Id,
                    Name = user.Organization.Name,
                    Description = user.Organization.Description
                } : null,
                Email = user.Email,
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

        var permissions = await GetUserPermissionsAsync(cancellationToken);
        return permissions.Contains(permission);
    }

    public async Task<bool> HasAnyPermissionAsync(IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        if (permissions == null || !permissions.Any())
        {
            return false;
        }

        var userPermissions = await GetUserPermissionsAsync(cancellationToken);
        return permissions.Any(p => userPermissions.Contains(p));
    }

    public async Task<bool> HasAllPermissionsAsync(IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        if (permissions == null || !permissions.Any())
        {
            return true; // No permissions required
        }

        var userPermissions = await GetUserPermissionsAsync(cancellationToken);
        return permissions.All(p => userPermissions.Contains(p));
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

    public async Task<bool> BelongsToOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user?.Organization?.Id == organizationId;
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
            var userRegionsQuery = _userRegionRepository.Query(ur => ur.User.AuthUserId == user.AuthUserId)
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

        try
        {
            var permissionsQuery = from rp in _dbContext.Set<Domain.Aggregates.RolePermission>()
                                   join p in _dbContext.Set<Domain.Entities.Permission>() on rp.PermissionId equals p.Id
                                   where rp.RoleId == user.Role.Id && !rp.IsDeleted && !p.IsDeleted
                                   select p.Key;

            _cachedPermissions = await permissionsQuery.ToListAsync(cancellationToken);
            return _cachedPermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for user: {UserId}", user.Id);
            _cachedPermissions = [];
            return _cachedPermissions;
        }
    }

    public async Task<Guid?> GetUserOrganizationIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user?.Organization?.Id;
    }
}