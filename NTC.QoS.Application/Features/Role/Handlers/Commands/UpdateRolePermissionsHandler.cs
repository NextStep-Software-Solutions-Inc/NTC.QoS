using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Commands;

public class UpdateRolePermissionsHandler(
    IRepository<Domain.Entities.Role> roleRepository,
    IUserAuthorizationService userAuthorizationService) : IRequestHandler<UpdateRolePermissionsRequest, ApiResponse<RoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;

    public async Task<ApiResponse<RoleDto>> Handle(UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var roleQuery = _roleRepository.Query(r => r.Id == request.RoleId);
        var role = await _roleRepository.FirstOrDefaultAsync(roleQuery, cancellationToken);

        if (role is null)
        {
            return new ApiResponse<RoleDto>("Role not found.");
        }

        // If system role, only Super Admin can edit permissions
        if (role.IsSystemRole)
        {
            var isSuperAdmin = await _userAuthorizationService.HasRoleAsync(Roles.SuperAdmin, cancellationToken);
            if (!isSuperAdmin)
            {
                return new ApiResponse<RoleDto>
                {
                    Succeeded = false,
                    Message = "Only Super Administrators can modify permissions of system roles.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Authorization", ["Permission denied: System roles are protected."] }
                    }
                };
            }
        }

        // Validate that all permissions exist in the static registry
        var invalidPermissions = request.Payload.Permissions
            .Where(p => !PermissionsRegistry.IsValidPermission(p))
            .ToList();

        if (invalidPermissions.Count > 0)
        {
            return new ApiResponse<RoleDto>
            {
                Succeeded = false,
                Message = $"Invalid permission(s): {string.Join(", ", invalidPermissions)}",
                Errors = new Dictionary<string, string[]>
                {
                    { "Permissions", invalidPermissions.ToArray() }
                }
            };
        }

        // If regional role, forbid platform-only permissions
        if (role.Scope == Roles.ScopeRegional)
        {
            var disallowed = request.Payload.Permissions
                .Where(p => !PermissionsRegistry.IsAllowedForRegionalScope(p))
                .ToList();

            if (disallowed.Count > 0)
            {
                return new ApiResponse<RoleDto>
                {
                    Succeeded = false,
                    Message = $"The following permissions cannot be assigned to regional roles: {string.Join(", ", disallowed)}",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Permissions", disallowed.ToArray() }
                    }
                };
            }
        }

        role.Permissions = request.Payload.Permissions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _roleRepository.Update(role);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RoleDto>
        {
            Succeeded = true,
            Message = "Role permissions updated successfully.",
            Data = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Key = role.Key,
                Description = role.Description ?? string.Empty,
                IsSystemRole = role.IsSystemRole,
                Scope = role.Scope,
                RegionId = role.RegionId,
                Permissions = role.Permissions
            }
        };
    }
}
