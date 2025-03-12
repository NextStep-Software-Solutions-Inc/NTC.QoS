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
}
