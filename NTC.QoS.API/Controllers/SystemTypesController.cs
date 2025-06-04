using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.SystemType.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("system-types", Name = "system-types")]
[Authorize]

public class SystemTypesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SystemTypeDto>>> CreateSystemType(CreateSystemTypeDto createSystemType)
    {
        var request = new CreateSystemTypeRequest { SystemType = createSystemType };
        var systemType = await _mediator.Send(request);
        return systemType.Succeeded ? Ok(systemType) : BadRequest(systemType);
    }
}
