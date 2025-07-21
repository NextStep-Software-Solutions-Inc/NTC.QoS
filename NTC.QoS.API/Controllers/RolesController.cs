using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("roles", Name = "roles")]
[Authorize]

public class RolesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole(CreateRoleDto createRoleDto)
    {
        var role = await _mediator.Send(new CreateRoleRequest(createRoleDto));
        return role.Succeeded ? Ok(role) : BadRequest(role);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RoleDto>>> GetAllRoles(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllRolesRequest(pageSize, pageNumber));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRoleById(Guid id)
    {
        var role = await _mediator.Send(new GetRoleByIdRequest(id));
        return role.Succeeded ? Ok(role) : BadRequest(role);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRoleById(Guid id, UpdateRoleDto updateRoleDto)
    {
        var role = await _mediator.Send(new UpdateRoleByIdRequest(id, updateRoleDto));
        return role.Succeeded ? Ok(role) : BadRequest(role);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRoleById(Guid id)
    {
        var role = await _mediator.Send(new DeleteRoleByIdRequest(id));
        return role.Succeeded ? Ok(role) : BadRequest(role);
    }
}
