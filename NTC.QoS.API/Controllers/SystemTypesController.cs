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

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SystemTypeDto>>> GetAllSystemTypes(
        int pageSize = 10,
        int pageNumber = 1,
        [FromQuery] List<Guid>? technologyIds = null,
        [FromQuery] List<Guid>? generationIds = null)
    {
        var systemType = await _mediator.Send(new GetAllSystemTypesRequest(pageSize, pageNumber, technologyIds, generationIds));
        return systemType.Succeeded ? Ok(systemType) : BadRequest(systemType);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SystemTypeDto>>> GetSystemTypeById(Guid id)
    {
        var systemType = await _mediator.Send(new GetSystemTypeByIdRequest(id));
        return systemType.Succeeded ? Ok(systemType) : BadRequest(systemType);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<SystemTypeDto>>> UpdateSystemTypeById(Guid id, UpdateSystemTypeDto updateSystemType)
    {
        var systemType = await _mediator.Send(new UpdateSystemTypeByIdRequest(id, updateSystemType));
        return systemType.Succeeded ? Ok(systemType) : BadRequest(systemType);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSystemTypeById(Guid id)
    {
        var systemType = await _mediator.Send(new DeleteSystemTypeByIdRequest(id));
        return systemType.Succeeded ? Ok(systemType) : BadRequest(systemType);
    }
}
