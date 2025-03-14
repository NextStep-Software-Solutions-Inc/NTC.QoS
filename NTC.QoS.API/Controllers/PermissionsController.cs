using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("permissions", Name = "permissions")]
public class PermissionsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> CreatePermission(CreatePermissionDto permissionDto)
    {
        var permission = await _mediator.Send(new CreatePermissionRequest
        {
            Permission = permissionDto
        });
        return permission.Succeeded ? Ok(permission) : BadRequest(permission);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<GetPermissionDto>>> GetAllPermissions(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllPermissionsRequest(pageSize, pageNumber));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<GetPermissionDto>>> GetPermissionById(Guid id)
    {
        var permission = await _mediator.Send(new GetPermissionByIdRequest(id));
        return permission.Succeeded ? Ok(permission) : BadRequest(permission);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UpdatePermissionDto>>> UpdatePermissionById(Guid id, UpdatePermissionDto updatePermission)
    {
        var permission = await _mediator.Send(new UpdatePermissionByIdRequest
        {
            Id = id,
            Permission = updatePermission
        });
        return permission.Succeeded ? Ok(permission) : BadRequest(permission);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePermissionById(Guid id)
    {
        var permission = await _mediator.Send(new DeletePermissionByIdRequest(id));
        return permission.Succeeded ? Ok(permission) : BadRequest(permission);
    }
}
