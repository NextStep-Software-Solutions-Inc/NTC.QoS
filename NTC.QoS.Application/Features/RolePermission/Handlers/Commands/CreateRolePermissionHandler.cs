using MediatR;
using NTC.QoS.Application.Features.RolePermission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.Application.Features.RolePermission.Handlers.Commands;
public class CreateRolePermissionHandler(
    IRepository<Domain.Aggregates.RolePermission> rolePermissionRepository,
    IRepository<Domain.Entities.Role> roleRepository,
    IRepository<Domain.Entities.Permission> permissionRepository)
    : IRequestHandler<CreateRolePermissionRequest, ApiResponse<RolePermissionDto>>
{
    private readonly IRepository<Domain.Aggregates.RolePermission> _rolePermissionRepository = rolePermissionRepository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Permission> _permissionRepository = permissionRepository;

    public async Task<ApiResponse<RolePermissionDto>> Handle(CreateRolePermissionRequest request, CancellationToken cancellationToken)
    {
        if (request.RolePermission.RoleId == Guid.Empty)
        {
            return new ApiResponse<RolePermissionDto>
            {
                Message = "RoleId cannot be empty.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]> { { "RoleId", ["RoleId cannot be an empty GUID."] } }
            };
        }
        if (request.RolePermission.PermissionId == Guid.Empty)
        {
            return new ApiResponse<RolePermissionDto>
            {
                Message = "PermissionId cannot be empty.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]> { { "PermissionId", ["PermissionId cannot be an empty GUID."] } }
            };
        }

        var existingRolePermissionQuery = _rolePermissionRepository.Query(rp =>
                rp.RoleId == request.RolePermission.RoleId &&
                rp.PermissionId == request.RolePermission.PermissionId);
        var existingRolePermission = await _rolePermissionRepository.FirstOrDefaultAsync(existingRolePermissionQuery, cancellationToken);


        if (existingRolePermission is not null)
        {
            if (existingRolePermission.IsDeleted)
            {
                existingRolePermission.IsDeleted = false;
                existingRolePermission.DeletedAt = null;

                _rolePermissionRepository.Update(existingRolePermission); 
                await _rolePermissionRepository.SaveChangesAsync(cancellationToken);

                var roleQuery = _roleRepository.Query(r => r.Id == existingRolePermission.RoleId);
                var role = await _roleRepository.FirstOrDefaultAsync(roleQuery, cancellationToken);

                var permissionQuery = _permissionRepository.Query(p => p.Id == existingRolePermission.PermissionId);
                var permission = await _permissionRepository.FirstOrDefaultAsync(permissionQuery, cancellationToken);

                return new ApiResponse<RolePermissionDto>
                {
                    Data = new RolePermissionDto
                    {
                        Id = existingRolePermission.Id,
                        Role = new ()
                        {
                            Id = existingRolePermission.RoleId,
                            Name = role?.Name ?? string.Empty,
                            Key = role?.Key ?? string.Empty,
                            Description = role?.Description
                        },
                        Permission = new ()
                        {
                            Id = existingRolePermission.PermissionId,
                            Name = permission?.Name ?? string.Empty,
                            Key = permission?.Key ?? string.Empty,
                            Description = permission?.Description,
                        }
                    },
                    Message = "Role permission reactivated successfully.",
                    Succeeded = true
                };
            }
            else
            {
                return new ApiResponse<RolePermissionDto>
                {
                    Message = "Role permission already exists and is active.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Duplicate", ["Role permission with the same RoleId and PermissionId already exists and is active."] }
                    }
                };
            }
        }

        var roleQueryForNew = _roleRepository.Query(r => r.Id == request.RolePermission.RoleId);
        var roleEntity = await _roleRepository.FirstOrDefaultAsync(roleQueryForNew, cancellationToken);
        if (roleEntity is null)
        {
            return new ApiResponse<RolePermissionDto>
            {
                Message = "Role not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "RoleId", ["Role with the specified RoleId does not exist."] }
                }
            };
        }

        var permissionQueryForNew = _permissionRepository.Query(p => p.Id == request.RolePermission.PermissionId);
        var permissionEntity = await _permissionRepository.FirstOrDefaultAsync(permissionQueryForNew, cancellationToken);
        if (permissionEntity is null)
        {
            return new ApiResponse<RolePermissionDto>
            {
                Message = "Permission not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "PermissionId", ["Permission with the specified PermissionId does not exist."] }
                }
            };
        }

        var newRolePermission = new Domain.Aggregates.RolePermission
        {
            RoleId = request.RolePermission.RoleId,
            PermissionId = request.RolePermission.PermissionId,
            IsDeleted = false
        };

        await _rolePermissionRepository.AddAsync(newRolePermission, cancellationToken); 
        await _rolePermissionRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RolePermissionDto>
        {
            Data = new RolePermissionDto
            {
                Id = newRolePermission.Id,
                Role = new ()
                {
                    Id = newRolePermission.RoleId,
                    Name = roleEntity.Name,
                    Key = roleEntity.Key,
                    Description = roleEntity.Description
                },
                Permission = new ()
                {
                    Id = newRolePermission.PermissionId,
                    Name = permissionEntity.Name,
                    Key = permissionEntity.Key,
                    Description = permissionEntity.Description,
                }
            },
            Message = "Role permission created successfully.",
            Succeeded = true
        };
    }
}