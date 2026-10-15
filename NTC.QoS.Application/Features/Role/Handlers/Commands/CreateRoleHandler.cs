using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Commands;

public class CreateRoleHandler(
    IRepository<Domain.Entities.Role> roleRepository,
    IUserAuthorizationService userAuthorizationService) : IRequestHandler<CreateRoleRequest, ApiResponse<RoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;

    public async Task<ApiResponse<RoleDto>> Handle(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var isSuperAdmin = await _userAuthorizationService.HasRoleAsync(Roles.SuperAdmin, cancellationToken);

        // Normalize scope
        var scope = string.IsNullOrWhiteSpace(request.Role.Scope) ? Roles.ScopeRegional : request.Role.Scope.Trim();
        if (!string.Equals(scope, Roles.ScopePlatform, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(scope, Roles.ScopeRegional, StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResponse<RoleDto>
            {
                Succeeded = false,
                Message = $"Invalid scope '{scope}'. Allowed scopes are '{Roles.ScopePlatform}' and '{Roles.ScopeRegional}'."
            };
        }

        // Scope restrictions for non-super admins
        if (!isSuperAdmin)
        {
            if (string.Equals(scope, Roles.ScopePlatform, StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<RoleDto>
                {
                    Succeeded = false,
                    Message = "Only Super Administrators can create Platform-scoped roles."
                };
            }

            if (!request.Role.RegionId.HasValue)
            {
                return new ApiResponse<RoleDto>
                {
                    Succeeded = false,
                    Message = "Regional roles must specify a valid RegionId."
                };
            }

            var hasRegionAccess = await _userAuthorizationService.HasRegionAccessAsync(request.Role.RegionId.Value, cancellationToken);
            if (!hasRegionAccess)
            {
                return new ApiResponse<RoleDto>
                {
                    Succeeded = false,
                    Message = "You do not have administrative access to the specified region."
                };
            }
        }

        // Key uniqueness check
        var normalizedKey = request.Role.Key.Trim().ToLowerInvariant().Replace(" ", "_");
        var existingRoleQuery = _roleRepository.QueryWithIgnoreQueryFilters(r =>
            r.Key.ToLower() == normalizedKey || r.Name.ToLower() == request.Role.Name.Trim().ToLower());
        var existingRole = await _roleRepository.FirstOrDefaultAsync(existingRoleQuery, cancellationToken);

        if (existingRole is not null)
        {
            if (existingRole.IsDeleted)
            {
                existingRole.IsDeleted = false;
                existingRole.Description = request.Role.Description ?? existingRole.Description;
                existingRole.Scope = scope;
                existingRole.RegionId = request.Role.RegionId;
                existingRole.Permissions = (request.Role.Permissions ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                _roleRepository.Update(existingRole);
                await _roleRepository.SaveChangesAsync(cancellationToken);

                return new ApiResponse<RoleDto>
                {
                    Data = new RoleDto
                    {
                        Id = existingRole.Id,
                        Name = existingRole.Name,
                        Key = existingRole.Key,
                        Description = existingRole.Description ?? string.Empty,
                        IsSystemRole = existingRole.IsSystemRole,
                        Scope = existingRole.Scope,
                        RegionId = existingRole.RegionId,
                        Permissions = existingRole.Permissions
                    },
                    Succeeded = true,
                    Message = "Role restored successfully."
                };
            }

            return new ApiResponse<RoleDto>
            {
                Message = "Role with the same name or key already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", ["Role with the same name or key already exists."] }
                }
            };
        }

        // Validate permissions
        var permissions = request.Role.Permissions ?? [];
        var invalidPermissions = permissions.Where(p => !PermissionsRegistry.IsValidPermission(p)).ToList();
        if (invalidPermissions.Count > 0)
        {
            return new ApiResponse<RoleDto>
            {
                Succeeded = false,
                Message = $"Invalid permission(s): {string.Join(", ", invalidPermissions)}"
            };
        }

        if (string.Equals(scope, Roles.ScopeRegional, StringComparison.OrdinalIgnoreCase))
        {
            var disallowed = permissions.Where(p => !PermissionsRegistry.IsAllowedForRegionalScope(p)).ToList();
            if (disallowed.Count > 0)
            {
                return new ApiResponse<RoleDto>
                {
                    Succeeded = false,
                    Message = $"The following permissions cannot be assigned to regional roles: {string.Join(", ", disallowed)}"
                };
            }
        }

        var newRole = new Domain.Entities.Role
        {
            Name = request.Role.Name.Trim(),
            Key = normalizedKey,
            Description = request.Role.Description,
            IsSystemRole = false,
            Scope = scope,
            RegionId = request.Role.RegionId,
            Permissions = permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };

        _roleRepository.Add(newRole);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RoleDto>
        {
            Data = new RoleDto
            {
                Id = newRole.Id,
                Name = newRole.Name,
                Key = newRole.Key,
                Description = newRole.Description ?? string.Empty,
                IsSystemRole = newRole.IsSystemRole,
                Scope = newRole.Scope,
                RegionId = newRole.RegionId,
                Permissions = newRole.Permissions
            },
            Succeeded = true,
            Message = "Role created successfully.",
        };
    }
}
