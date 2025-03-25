using MediatR;
using NTC.QoS.Application.Features.RolePermission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.Application.Features.RolePermission.Handlers.Queries;

public class GetRolePermissionByIdHandler(IRolePermissionRepository rolePermissionRepository) : IRequestHandler<GetRolePermissionByIdRequest, ApiResponse<GetRolePermissionDto>>
{
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;
    public async Task<ApiResponse<GetRolePermissionDto>> Handle(GetRolePermissionByIdRequest request, CancellationToken cancellationToken)
    {
        var rolePermission = await _rolePermissionRepository.GetByIdAsync(request.Id, cancellationToken);
        if (rolePermission is null)
        {
            return new ApiResponse<GetRolePermissionDto>
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
        var result = new GetRolePermissionDto
        {
            Id = rolePermission.Id,
            Role =  new RolePermissionDto
            {
                Id = rolePermission.RoleId,
                Name = rolePermission.Role?.Name ?? string.Empty,
                Key = rolePermission.Role?.Key ?? string.Empty,
                Description = rolePermission.Role?.Description
            },
            Permission = new RolePermissionDto
            {
                Id = rolePermission.PermissionId,
                Name = rolePermission.Permission?.Name ?? string.Empty,
                Key = rolePermission.Permission?.Key ?? string.Empty,
                Description = rolePermission.Permission?.Description
            }
        };

        return new ApiResponse<GetRolePermissionDto>
        {
            Data = result,
            Message = "Role permission retrieved successfully.",
            Succeeded = true
        };
    }
}
