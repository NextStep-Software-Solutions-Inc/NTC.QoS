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
}
