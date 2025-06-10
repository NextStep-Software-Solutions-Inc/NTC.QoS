using MediatR;
using NTC.QoS.Application.Features.RolePermission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.Application.Features.RolePermission.Handlers.Queries;

public class GetAllRolePermissionsHandler(IRolePermissionRepository rolePermissionRepository) : IRequestHandler<GetAllRolePermissionsRequest, PaginatedResponse<GetRolePermissionDto>>
{
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;
    public async Task<PaginatedResponse<GetRolePermissionDto>> Handle(GetAllRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var allRolePermissions = _rolePermissionRepository.Query(rp => !rp.IsDeleted);

        var rolePermissionDtos = allRolePermissions.Select(rp => new GetRolePermissionDto
        {
            Id = rp.Id,
            Role = new()
            {
                Id = rp.RoleId,
                Name = rp.Role != null ? rp.Role.Name : string.Empty,
                Key = rp.Role != null ? rp.Role.Key : string.Empty,
                Description = rp.Role != null ? rp.Role.Description : string.Empty
            },
            Permission = new()
            {
                Id = rp.PermissionId,
                Name = rp.Permission != null ? rp.Permission.Name : string.Empty,
                Key = rp.Permission != null ? rp.Permission.Key : string.Empty,
                Description = rp.Permission != null ? rp.Permission.Description : string.Empty
            }
        });

        if (request.RoleId.HasValue)
        {
            rolePermissionDtos = rolePermissionDtos.Where(rp => rp.Role.Id == request.RoleId);
        }

        if (request.PermissionId.HasValue)
        {
            rolePermissionDtos = rolePermissionDtos.Where(rp => rp.Permission.Id == request.PermissionId);
        }

        return await _rolePermissionRepository.PaginateAsync(rolePermissionDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
