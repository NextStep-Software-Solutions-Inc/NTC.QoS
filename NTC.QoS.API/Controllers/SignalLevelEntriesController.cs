using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.SignalLevelEntry.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("signal-levels/{signalLevelId:guid}/entries", Name = "signal-level-entries")]
[Authorize]
public class SignalLevelEntriesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [RequirePermission(Permissions.CreateSignalLevelEntries)]
    public async Task<ActionResult<ApiResponse<SignalLevelEntryDto>>> CreateSignalLevelEntry(Guid signalLevelId, CreateSignalLevelEntryDto signalLevelEntryDto)
    {
        var result = await _mediator.Send(new CreateSignalLevelEntryRequest(signalLevelId, signalLevelEntryDto));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewSignalLevelEntries)]
    public async Task<ActionResult<PaginatedResponse<SignalLevelEntryDto>>> GetAllSignalLevelEntries(Guid signalLevelId,
                                                                                                  [FromQuery] int pageSize = 10,
                                                                                                  [FromQuery] int pageNumber = 1)
    {
        var result = await _mediator.Send(new GetAllSignalLevelEntriesRequest(signalLevelId, pageSize, pageNumber));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.ViewSignalLevelEntries)]
    public async Task<ActionResult<ApiResponse<SignalLevelEntryDto>>> GetSignalLevelEntryById(Guid signalLevelId, Guid id)
    {
        var result = await _mediator.Send(new GetSignalLevelEntryByIdRequest(signalLevelId, id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}")]
    [RequirePermission(Permissions.UpdateSignalLevelEntries)]
    public async Task<ActionResult<ApiResponse<SignalLevelEntryDto>>> UpdateSignalLevelEntryById(Guid signalLevelId, Guid id, UpdateSignalLevelEntryDto updateSignalLevelEntry)
    {
        var result = await _mediator.Send(new UpdateSignalLevelEntryByIdRequest(signalLevelId, id, updateSignalLevelEntry));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.DeleteSignalLevelEntries)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSignalLevelEntryById(Guid signalLevelId, Guid id)
    {
        var result = await _mediator.Send(new DeleteSignalLevelEntryByIdRequest(signalLevelId, id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}