using MediatR;
using NTC.QoS.Application.Features.RolePermission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.Application.Features.RolePermission.Handlers.Queries;

public class GetRolePermissionByIdHandler(IRolePermissionRepository rolePermissionRepository) : IRequestHandler<GetRolePermissionByIdRequest, ApiResponse<RolePermissionDto>>
{
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;
    public async Task<ApiResponse<RolePermissionDto>> Handle(GetRolePermissionByIdRequest request, CancellationToken cancellationToken)
    {
        var rolePermissionQuery = _rolePermissionRepository.Query(r => r.Id.Equals(request.Id));
        var rolePermission = await _rolePermissionRepository.FirstOrDefaultAsync(rolePermissionQuery, cancellationToken);

        if (rolePermission is null)
        {
            return new ApiResponse<RolePermissionDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Id", [$"Role permission with ID [{request.Id}] not found."]
                    }
                },
                Message = $"Role permission does not exist."
            };
        }
        var result = new RolePermissionDto
        {
            Id = rolePermission.Id,
            Role =  new ()
            {
                Id = rolePermission.RoleId,
                Name = rolePermission.Role?.Name ?? string.Empty,
                Key = rolePermission.Role?.Key ?? string.Empty,
                Description = rolePermission.Role?.Description
            },
            Permission = new ()
            {
                Id = rolePermission.PermissionId,
                Name = rolePermission.Permission?.Name ?? string.Empty,
                Key = rolePermission.Permission?.Key ?? string.Empty,
                Description = rolePermission.Permission?.Description
            }
        };

        return new ApiResponse<RolePermissionDto>
        {
            Data = result,
            Message = "Role permission retrieved successfully.",
            Succeeded = true
        };
    }
}
