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
                    { "Id", [$"Role permission with ID [{request.Id}] not found."] }
                },
                Message = $"Role permission does not exist."
            };
        }
        var result = new GetRolePermissionDto
        {
            Id = rolePermission.Id,
            Role = rolePermission.Role is not null ? new RolePermissionDto
            {
                Id = rolePermission.Role.Id,
                Name = rolePermission.Role.Name,
                Key = rolePermission.Role.Key,
                Description = rolePermission.Role.Description
            } : null,
            Permission = rolePermission.Permission is not null ? new RolePermissionDto
            {
                Id = rolePermission.Permission.Id,
                Name = rolePermission.Permission.Name,
                Key = rolePermission.Permission.Key,
                Description = rolePermission.Permission.Description
            } : null
        };

        return new ApiResponse<GetRolePermissionDto>
        {
            Data = result,
            Message = "Role permission retrieved successfully.",
            Succeeded = true
        };
    }
}
