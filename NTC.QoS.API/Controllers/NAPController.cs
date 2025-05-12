using System.ComponentModel.DataAnnotations;
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

    [HttpPost]
    public async Task<ActionResult<ApiResponse<NAPDto>>> CreateNAP(CreateNAPDto createNAPDto)
    {
        var NAP = await _mediator.Send(new CreateNAPRequest(createNAPDto));

        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<NAPDto>>> GetAllNAPs(
        int pageSize = 10,
        int pageNumber = 1,
        Guid? serviceProviderId = null,
        double longitude = 0,
        double latitude = 0,
        double distance = 1000)
    {

        var NAP = await _mediator.Send(new GetAllNAPsRequest(pageSize: pageSize, pageNumber: pageNumber)
        {
            ServiceProviderId = serviceProviderId,
            Coordinate = new (y: longitude, x: latitude),
            Distance = distance
        });

        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<NAPDto>>> GetNAPById(Guid id)
    {
        var request = new GetNAPByIdRequest(id);
        var NAP = await _mediator.Send(request);
        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<Domain.Aggregates.NAP>>> DeleteNAPById(Guid id)
    {
        var NAP = await _mediator.Send(new DeleteNAPByIdRequest(id));

        return NAP.Succeeded ? Ok(NAP) : BadRequest(NAP);
    }
}
