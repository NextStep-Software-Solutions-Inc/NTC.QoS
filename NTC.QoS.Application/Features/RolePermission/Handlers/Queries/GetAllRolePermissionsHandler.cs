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

        return await _rolePermissionRepository.PaginateAsync(rolePermissionDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}