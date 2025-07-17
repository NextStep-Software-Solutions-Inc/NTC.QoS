using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("bands", Name = "bands")]
public class BandsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BandsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BandDto>>> GetAllBands(int pageSize = 10, int pageNumber = 1, [FromQuery] List<Guid>? technologies = default)
    {
        var result = await _mediator.Send(new GetAllBandsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Technologies = technologies
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BandDto>>> GetBandById(Guid id)
    {
        var request = new GetBandByIdRequest(id);
        var band = await _mediator.Send(request);
        return band.Succeeded ? Ok(band) : BadRequest(band);
    }

    [HttpPost]
    public async Task<IActionResult> Post(CreateBandDto bandDto)
    {
        var band = await _mediator.Send(new CreateBandRequest
        {
            Band = bandDto
        });

        return band.Succeeded ? Ok(band) : BadRequest(band);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UpdateBandDto>>> UpdateBandById(Guid id, UpdateBandDto updateBand)
    {
        var band = await _mediator.Send(new UpdateBandByIdRequest
        {
            Id = id,
            Band = updateBand
        });

        return band.Succeeded ? Ok(band) : BadRequest(band);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteBandById(Guid id)
    {
        var band = await _mediator.Send(new DeleteBandByIdRequest(id));

        return band.Succeeded ? Ok(band) : BadRequest(band);
    }
}
