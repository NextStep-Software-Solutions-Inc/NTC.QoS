using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Commands;

public class UpdateRoleByIdHandler(
    IRepository<Domain.Entities.Role> repository,
    IUserAuthorizationService userAuthorizationService) : IRequestHandler<UpdateRoleByIdRequest, ApiResponse<RoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _repository = repository;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;

    public async Task<ApiResponse<RoleDto>> Handle(UpdateRoleByIdRequest request, CancellationToken cancellationToken)
    {
        var roleQuery = _repository.Query(r => r.Id == request.Id);
        var existingRole = await _repository.FirstOrDefaultAsync(roleQuery, cancellationToken);

        if (existingRole is null)
        {
            return new ApiResponse<RoleDto>("Role not found.");
        }

        var isSuperAdmin = await _userAuthorizationService.HasRoleAsync(Roles.SuperAdmin, cancellationToken);

        // System role protection: only super admin can modify system roles
        if (existingRole.IsSystemRole && !isSuperAdmin)
        {
            return new ApiResponse<RoleDto>
            {
                Succeeded = false,
                Message = "Only Super Administrators can modify system roles.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Authorization", ["Permission denied: System roles are protected."] }
                }
            };
        }

        // Non-super admin cannot modify roles outside their regional jurisdiction
        if (!isSuperAdmin)
        {
            if (existingRole.RegionId.HasValue)
            {
                var hasAccess = await _userAuthorizationService.HasRegionAccessAsync(existingRole.RegionId.Value, cancellationToken);
                if (!hasAccess)
                {
                    return new ApiResponse<RoleDto>
                    {
                        Succeeded = false,
                        Message = "You do not have administrative access to modify roles in this region."
                    };
                }
            }

            // Cannot elevate scope to Platform
            if (!string.IsNullOrEmpty(request.Role.Scope) &&
                string.Equals(request.Role.Scope, Roles.ScopePlatform, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(existingRole.Scope, Roles.ScopePlatform, StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<RoleDto>
                {
                    Succeeded = false,
                    Message = "Only Super Administrators can assign Platform scope."
                };
            }
        }

        // System roles cannot change key or scope
        if (!existingRole.IsSystemRole)
        {
            if (!string.IsNullOrEmpty(request.Role.Key))
            {
                existingRole.Key = request.Role.Key.Trim().ToLowerInvariant().Replace(" ", "_");
            }

            if (!string.IsNullOrEmpty(request.Role.Scope))
            {
                existingRole.Scope = request.Role.Scope;
            }

            if (request.Role.RegionId.HasValue)
            {
                existingRole.RegionId = request.Role.RegionId;
            }
        }

        existingRole.Name = request.Role.Name ?? existingRole.Name;
        existingRole.Description = request.Role.Description ?? existingRole.Description;

        // If permissions are updated via role update DTO
        if (request.Role.Permissions != null)
        {
            var invalidPermissions = request.Role.Permissions
                .Where(p => !PermissionsRegistry.IsValidPermission(p))
                .ToList();

            if (invalidPermissions.Count > 0)
            {
                return new ApiResponse<RoleDto>
                {
                    Succeeded = false,
                    Message = $"Invalid permission(s): {string.Join(", ", invalidPermissions)}"
                };
            }

            if (existingRole.Scope == Roles.ScopeRegional)
            {
                var disallowed = request.Role.Permissions
                    .Where(p => !PermissionsRegistry.IsAllowedForRegionalScope(p))
                    .ToList();

                if (disallowed.Count > 0)
                {
                    return new ApiResponse<RoleDto>
                    {
                        Succeeded = false,
                        Message = $"The following permissions cannot be assigned to regional roles: {string.Join(", ", disallowed)}"
                    };
                }
            }

            existingRole.Permissions = request.Role.Permissions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        _repository.Update(existingRole);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RoleDto>
        {
            Succeeded = true,
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
            Message = "Role updated successfully.",
        };
    }
}
