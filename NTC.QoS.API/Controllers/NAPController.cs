using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("NAPs", Name = "NAPs")]
public class NAPsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NAPsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllNAPs(
        int pageSize = 10,
        int pageNumber = 1,
        Guid? serviceProviderId = null, 
        Guid? zoneId = null,
        Guid? fileId = null)
    {
        var result = await _mediator.Send(new GetAllNAPsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ServiceProviderId = serviceProviderId,
            ZoneId = zoneId,
            FileId = fileId
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<NAPDto>>> GetNAPById(Guid id)
    {
        var request = new GetNAPByIdRequest(id);
        var NAP = await _mediator.Send(request);
        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }

    [HttpPost]
    public async Task<IActionResult> Post(CreateNAPDto createNAPDto)
    {
        var NAP = await _mediator.Send(new CreateNAPRequest
        {
            CreateNAP = createNAPDto
        });

        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Aggregates.NAP>>> DeleteNAPById(Guid id)
    {
        var NAP = await _mediator.Send(new DeleteNAPByIdRequest(id));

        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }
}
