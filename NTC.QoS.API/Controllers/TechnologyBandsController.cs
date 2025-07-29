using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.TechnologyBand.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.TechnologyBand;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("technology-bands", Name = "technology-bands")]
[Authorize]
public class TechnologyBandsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TechnologyBandsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TechnologyBandDto>>> GetAllTechnologyBands(
        int pageSize = 10, 
        int pageNumber = 1, 
        [FromQuery] List<Guid>? technologies = default,
        [FromQuery] List<Guid>? bands = default)
    {
        var result = await _mediator.Send(new GetAllTechnologyBandsRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Technologies = technologies,
            Bands = bands
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TechnologyBandDto>>> GetTechnologyBandById(Guid id)
    {
        var request = new GetTechnologyBandByIdRequest(id);
        var technologyBand = await _mediator.Send(request);
        return technologyBand.Succeeded ? Ok(technologyBand) : BadRequest(technologyBand);
    }

    [HttpPost]
    public async Task<IActionResult> Post(CreateTechnologyBandDto technologyBandDto)
    {
        var technologyBand = await _mediator.Send(new CreateTechnologyBandRequest
        {
            TechnologyBand = technologyBandDto
        });

        return technologyBand.Succeeded ? Ok(technologyBand) : BadRequest(technologyBand);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteTechnologyBandByIdRequest(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
