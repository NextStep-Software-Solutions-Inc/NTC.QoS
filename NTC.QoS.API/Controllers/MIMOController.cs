using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.MIMO.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MIMO;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("mimos", Name = "mimos")]
public class MIMOsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MIMOsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<MIMODto>>> GetAllMIMOs(int pageSize = 10, int pageNumber = 1)
    {
        var request = new GetAllMIMOsRequest()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        var mimos = await _mediator.Send(request);
        return Ok(mimos);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MIMODto>>> GetMIMOById(Guid id)
    {
        var request = new GetMIMOByIdRequest(id);
        var mimo = await _mediator.Send(request);
        return mimo.Succeeded ? Ok(mimo) : BadRequest(mimo);
    }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateMIMODto>>> Post(CreateMIMODto mimoDto)
    {
        var mimo = await _mediator.Send(new CreateMIMORequest(mimoDto));
        return mimo.Succeeded ? Ok(mimo) : BadRequest(mimo);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var request = new DeleteMIMOByIdRequest(id);
        var mimo = await _mediator.Send(request);

        return mimo.Succeeded ? Ok(mimo) : BadRequest(mimo);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.MIMO>>> UpdateMIMOById(Guid id, UpdateMIMODto updateMIMO)
    {
        var mimo = await _mediator.Send(new UpdateMIMOByIdRequest(id, new UpdateMIMODto
        {
            Type = updateMIMO.Type,
            DLCoverageGain = updateMIMO.DLCoverageGain,
            ULCoverageGain = updateMIMO.ULCoverageGain,
            DLInterferenceReduction = updateMIMO.DLInterferenceReduction,
            ULInterferenceReduction = updateMIMO.ULInterferenceReduction,
            DLThroughput = updateMIMO.DLInterferenceReduction,
            ULThroughput = updateMIMO.ULThroughput,
        }));
        return mimo.Succeeded ? Ok(mimo) : BadRequest(mimo);
    }
}
