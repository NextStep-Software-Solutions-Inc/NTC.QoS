using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.RolePermission.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.RolePermission;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("role-permissions", Name = "role-permissions")]
public class RolePermissionsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateRolePermissionDto>>> CreateRolePermission(CreateRolePermissionDto rolePermissionDto)
    {
        var rolePermission = await _mediator.Send(new CreateRolePermissionRequest
        {
            RolePermission = rolePermissionDto
        });
        return rolePermission.Succeeded ? Ok(rolePermission) : BadRequest(rolePermission);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<GetRolePermissionDto>>> GetAllRolePermissions(int pageSize = 10,
                                                                                                   int pageNumber = 1,
                                                                                                   Guid? roleId = null,
                                                                                                   Guid? permissionId = null)
    {
        var rolePermissions = await _mediator.Send(new GetAllRolePermissionsRequest(pageNumber, pageSize, roleId, permissionId));
        return rolePermissions.Succeeded ? Ok(rolePermissions) : BadRequest(rolePermissions);
    }
}
