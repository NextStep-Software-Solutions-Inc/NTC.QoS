using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("roles", Name = "roles")]
public class RolesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> CreateRole(CreateRoleDto roleDto)
    {
        var role = await _mediator.Send(new CreateRoleRequest
        {
            Role = roleDto
        });
        return role.Succeeded ? Ok(role) : BadRequest(role);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<GetRoleDto>>> GetAllRoles(int pageSize = 10, int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllRolesRequest(pageSize, pageNumber));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<GetRoleDto>>> GetRoleById(Guid id)
    {
        var request = new GetRoleByIdRequest(id);
        var role = await _mediator.Send(request);
        return role.Succeeded ? Ok(role) : BadRequest(role);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UpdateRoleDto>>> UpdateRoleById(Guid id, UpdateRoleDto updateRole)
    {
        var role = await _mediator.Send(new UpdateRoleByIdRequest
        {
            Id = id,
            Role = updateRole
        });
        return role.Succeeded ? Ok(role) : BadRequest(role);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRoleById(Guid id)
    {
        var role = await _mediator.Send(new DeleteRoleByIdRequest(id));
        return role.Succeeded ? Ok(role) : BadRequest(role);
    }
}
