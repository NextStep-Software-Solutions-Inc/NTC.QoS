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
        var allRolePermissions = await _rolePermissionRepository.ListAsync(rp => !rp.IsDeleted, cancellationToken);
        
        if (request.RoleId.HasValue)
        {
            allRolePermissions = [.. allRolePermissions.Where(rp => rp.RoleId == request.RoleId)];
        }

        if (request.PermissionId.HasValue)
        {
            allRolePermissions = [.. allRolePermissions.Where(rp => rp.PermissionId == request.PermissionId)];
        }

        var rolePermissionDtos = allRolePermissions.Select(static rp => new GetRolePermissionDto
        {
            Id = rp.Id,
            Role = new RolePermissionDto
            {
                Id = rp.RoleId,
                Name = rp.Role?.Name ?? string.Empty,
                Key = rp.Role?.Key ?? string.Empty,
                Description = rp.Role?.Description ?? string.Empty
            },
            Permission = new RolePermissionDto
            {
                Id = rp.PermissionId,
                Name = rp.Permission?.Name ?? string.Empty,
                Key = rp.Permission?.Key ?? string.Empty,
                Description = rp.Permission?.Description ?? string.Empty
            }
        }).ToList();

        return new PaginatedResponse<GetRolePermissionDto>(rolePermissionDtos, request.PageNumber, request.PageSize)
        {
            Data = rolePermissionDtos,
            Succeeded = true,
            Message = "Role permissions retrieved successfully."
        };
    }
}
