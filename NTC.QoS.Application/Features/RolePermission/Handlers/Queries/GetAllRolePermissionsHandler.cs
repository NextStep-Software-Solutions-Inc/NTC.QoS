using MediatR;
using NTC.QoS.Application.Features.RolePermission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.Application.Features.RolePermission.Handlers.Queries;

public class GetAllRolePermissionsHandler(IRolePermissionRepository rolePermissionRepository)
    : IRequestHandler<GetAllRolePermissionsRequest, PaginatedResponse<RolePermissionDto>>
{
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;

    public async Task<PaginatedResponse<RolePermissionDto>> Handle(GetAllRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var query = _rolePermissionRepository.Query();

        if (request.RoleId.HasValue)
        {
            query = query.Where(rp => rp.RoleId == request.RoleId);
        }

        if (request.PermissionId.HasValue)
        {
            query = query.Where(rp => rp.PermissionId == request.PermissionId);
        }

        var rolePermissionDtos = query.Select(rp => new RolePermissionDto
        {
            Id = rp.Id,
            Role = new()
            {
                Id = rp.RoleId,
                Name = rp.Role!.Name,
                Key = rp.Role!.Key,
                Description = rp.Role!.Description
            },
            Permission = new()
            {
                Id = rp.PermissionId,
                Name = rp.Permission!.Name,
                Key = rp.Permission!.Key,
                Description = rp.Permission!.Description
            }
        });

        return await _rolePermissionRepository.PaginateAsync(rolePermissionDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}