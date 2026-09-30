using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("permissions", Name = "permissions")]
[Authorize]
public class PermissionsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [RequirePermission(Permissions.ViewPermissions)]
    public async Task<ActionResult<PaginatedResponse<PermissionDto>>> GetAllPermissions(int pageSize = 10, int pageNumber = 1, [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetAllPermissionsRequest(pageSize, pageNumber, search));
        return Ok(result);
    }
}
