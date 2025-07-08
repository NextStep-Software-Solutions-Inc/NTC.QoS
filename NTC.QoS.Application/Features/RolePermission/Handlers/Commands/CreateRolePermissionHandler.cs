using MediatR;
using NTC.QoS.Application.Features.RolePermission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.Application.Features.RolePermission.Handlers.Commands;

public class CreateRolePermissionHandler(
    IRolePermissionRepository rolePermissionRepository,
    IRepository<Domain.Entities.Role> roleRepository,
    IRepository<Domain.Entities.Permission> permissionRepository)
    : IRequestHandler<CreateRolePermissionRequest, ApiResponse<RolePermissionDto>>
{
    public async Task<ApiResponse<RolePermissionDto>> Handle(CreateRolePermissionRequest request, CancellationToken cancellationToken)
    {
        if (request.RolePermission.RoleId == Guid.Empty)
        {
            return new ApiResponse<RolePermissionDto>
            {
                Message = "RoleId cannot be empty.",
                Succeeded = false,
                Errors = new()
                {
                    { "RoleId", ["RoleId cannot be an empty GUID."] }
                }
            };
        }

        if (request.RolePermission.PermissionId == Guid.Empty)
        {
            return new ApiResponse<RolePermissionDto>
            {
                Message = "PermissionId cannot be empty.",
                Succeeded = false,
                Errors = new()
                {
                    { "PermissionId", ["PermissionId cannot be an empty GUID."] }
                }
            };
        }

        var existingQuery = rolePermissionRepository.Query(rp =>
            rp.RoleId == request.RolePermission.RoleId &&
            rp.PermissionId == request.RolePermission.PermissionId);

        var existing = await rolePermissionRepository.FirstOrDefaultAsync(existingQuery, cancellationToken);

        if (existing is not null)
        {
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;

                rolePermissionRepository.Update(existing);
                await rolePermissionRepository.SaveChangesAsync(cancellationToken);

                return new ApiResponse<RolePermissionDto>
                {
                    Data = new RolePermissionDto
                    {
                        Id = existing.Id,
                        Role = new()
                        {
                            Id = existing.RoleId,
                            Name = existing.Role?.Name ?? string.Empty,
                            Key = existing.Role?.Key ?? string.Empty,
                            Description = existing.Role?.Description
                        },
                        Permission = new()
                        {
                            Id = existing.PermissionId,
                            Name = existing.Permission?.Name ?? string.Empty,
                            Key = existing.Permission?.Key ?? string.Empty,
                            Description = existing.Permission?.Description
                        }
                    },
                    Message = "Role permission was previously deleted and has now been successfully reactivated.",
                    Succeeded = true
                };
            }
            else
            {
                return new ApiResponse<RolePermissionDto>
                {
                    Message = "Role permission already exists and is active.",
                    Succeeded = false,
                    Errors = new()
                    {
                        { "Duplicate", ["Role permission with the same RoleId and PermissionId already exists and is active."] }
                    }
                };
            }
        }

        var roleQuery = roleRepository.Query(r => r.Id == request.RolePermission.RoleId);
        var role = await roleRepository.FirstOrDefaultAsync(roleQuery, cancellationToken);
        if (role is null)
        {
            return new ApiResponse<RolePermissionDto>
            {
                Message = "Role not found.",
                Succeeded = false,
                Errors = new()
                {
                    { "RoleId", ["Role with the specified RoleId does not exist."] }
                }
            };
        }

        var permissionQuery = permissionRepository.Query(p => p.Id == request.RolePermission.PermissionId);
        var permission = await permissionRepository.FirstOrDefaultAsync(permissionQuery, cancellationToken);
        if (permission is null)
        {
            return new ApiResponse<RolePermissionDto>
            {
                Message = "Permission not found.",
                Succeeded = false,
                Errors = new()
                {
                    { "PermissionId", ["Permission with the specified PermissionId does not exist."] }
                }
            };
        }

        var newRolePermission = new Domain.Aggregates.RolePermission
        {
            RoleId = request.RolePermission.RoleId,
            PermissionId = request.RolePermission.PermissionId,
        };

        await rolePermissionRepository.AddAsync(newRolePermission, cancellationToken);
        await rolePermissionRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RolePermissionDto>
        {
            Data = new RolePermissionDto
            {
                Id = newRolePermission.Id,
                Role = new()
                {
                    Id = role.Id,
                    Name = role.Name,
                    Key = role.Key,
                    Description = role.Description
                },
                Permission = new()
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    Key = permission.Key,
                    Description = permission.Description
                }
            },
            Message = "Role permission created successfully.",
            Succeeded = true
        };
    }
}