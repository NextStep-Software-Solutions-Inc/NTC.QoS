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
    public async Task<ActionResult<ApiResponse<PermissionDto>>> CreatePermission(CreatePermissionDto createPermissionDto)
    {
        var permission = await _mediator.Send(new CreatePermissionRequest(createPermissionDto));
        return permission.Succeeded ? Ok(permission) : BadRequest(permission);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<PermissionDto>>> GetAllPermissions(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllPermissionsRequest(pageSize, pageNumber));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> GetPermissionById(Guid id)
    {
        var permission = await _mediator.Send(new GetPermissionByIdRequest(id));
        return permission.Succeeded ? Ok(permission) : BadRequest(permission);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> UpdatePermissionById(Guid id, UpdatePermissionDto updatePermissionDto)
    {
        var permission = await _mediator.Send(new UpdatePermissionByIdRequest(id, updatePermissionDto));
        return permission.Succeeded ? Ok(permission) : BadRequest(permission);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePermissionById(Guid id)
    {
        var permission = await _mediator.Send(new DeletePermissionByIdRequest(id));
        return permission.Succeeded ? Ok(permission) : BadRequest(permission);
    }
}
