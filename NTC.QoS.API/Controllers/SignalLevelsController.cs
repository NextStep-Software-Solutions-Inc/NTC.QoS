using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.SignalLevel.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("signal-levels", Name = "signal-levels")]
[Authorize]
public class SignalLevelsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [RequirePermission(Permissions.CreateSignalLevels)]
    public async Task<ActionResult<ApiResponse<SignalLevelDto>>> CreateSignalLevel(CreateSignalLevelDto signalLevelDto)
    {
        var result = await _mediator.Send(new CreateSignalLevelRequest(signalLevelDto));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewSignalLevels)]
    public async Task<ActionResult<PaginatedResponse<SignalLevelDto>>> GetAllSignalLevels([FromQuery] int pageSize = 10,
                                                                                          [FromQuery] int pageNumber = 1,
                                                                                          [FromQuery] Guid? areaStudyId = null)
    {
        var result = await _mediator.Send(new GetAllSignalLevelsRequest(pageSize, pageNumber, areaStudyId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.ViewSignalLevels)]
    public async Task<ActionResult<ApiResponse<SignalLevelDto>>> GetSignalLevelById(Guid id)
    {
        var result = await _mediator.Send(new GetSignalLevelByIdRequest(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}")]
    [RequirePermission(Permissions.UpdateSignalLevels)]
    public async Task<ActionResult<ApiResponse<SignalLevelDto>>> UpdateSignalLevelById(Guid id, UpdateSignalLevelDto updateSignalLevel)
    {
        var result = await _mediator.Send(new UpdateSignalLevelByIdRequest(id, updateSignalLevel));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.DeleteSignalLevels)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSignalLevelById(Guid id)
    {
        var result = await _mediator.Send(new DeleteSignalLevelByIdRequest(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}