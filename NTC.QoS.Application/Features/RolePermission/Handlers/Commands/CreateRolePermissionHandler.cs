using MediatR;
using NTC.QoS.Application.Features.RolePermission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.Application.Features.RolePermission.Handlers.Commands;

public class CreateRolePermissionHandler(IRepository<Domain.Aggregates.RolePermission> rolePermissionRepository,
                                         IRepository<Domain.Entities.Role> roleRepository,
                                         IRepository<Domain.Entities.Permission> permissionRepository) : IRequestHandler<CreateRolePermissionRequest, ApiResponse<RolePermissionDto>>
{
    private readonly IRepository<Domain.Aggregates.RolePermission> _rolePermissionRepository = rolePermissionRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Permission> _permissionRepository = permissionRepository;

    public async Task<ApiResponse<RolePermissionDto>> Handle(CreateRolePermissionRequest request, CancellationToken cancellationToken)
    {
        var existingRolePermissionQuery = _rolePermissionRepository.Query(rp => rp.RoleId == request.RolePermission.RoleId && rp.PermissionId == request.RolePermission.PermissionId);
        var existingRolePermission = await _rolePermissionRepository.FirstOrDefaultAsync(existingRolePermissionQuery, cancellationToken);

        if (existingRolePermission is not null)
        {
            return new ApiResponse<RolePermissionDto>
            {
                Message = "Role permission already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Duplicate", ["Role permission with the same RoleId and PermissionId already exists."]
                    }
                }
            };
        }

        if (request.RolePermission.RoleId != Guid.Empty)
        {
            var roleQuery = _roleRepository.Query(r => r.Id == request.RolePermission.RoleId);
            var role = await _roleRepository.FirstOrDefaultAsync(roleQuery, cancellationToken);

            if (role is null)
            {
                return new ApiResponse<RolePermissionDto>
                {
                    Message = "Role not found.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { 
                            "RoleId", ["Role with the specified RoleId does not exist."]
                        }
                    }
                };
            }
        }

        if (request.RolePermission.PermissionId != Guid.Empty)
        {
            var permissionQuery = _permissionRepository.Query(p => p.Id == request.RolePermission.PermissionId);
            var permission = await _permissionRepository.FirstOrDefaultAsync(permissionQuery, cancellationToken);

            if (permission is null)
            {
                return new ApiResponse<RolePermissionDto>
                {
                    Message = "Permission not found.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { 
                            "PermissionId", ["Permission with the specified PermissionId does not exist."]
                        }
                    }
                };
            }
        }
        var newRolePermission = new Domain.Aggregates.RolePermission
        {
            RoleId = request.RolePermission.RoleId,
            PermissionId = request.RolePermission.PermissionId
        };

        _rolePermissionRepository.Add(newRolePermission);
        await _rolePermissionRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RolePermissionDto>
        {
            Data = new RolePermissionDto
            {
                Role = new ()
                {
                    Id = newRolePermission.RoleId,
                    Name = newRolePermission.Role?.Name ?? string.Empty,
                    Key = newRolePermission.Role?.Key ?? string.Empty,
                    Description = newRolePermission.Role?.Description
                },
                Permission = new ()
                {
                    Id = newRolePermission.PermissionId,
                    Name = newRolePermission.Permission?.Name ?? string.Empty,
                    Key = newRolePermission.Permission?.Key ?? string.Empty,
                    Description = newRolePermission.Permission?.Description,
                }
            },
            Message = "Role permission created successfully.",
            Succeeded = true
        };
    }
}
