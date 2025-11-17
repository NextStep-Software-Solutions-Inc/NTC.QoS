using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Application.Features.Band.Queries.GetTechnologyBandsByLocation;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("bands", Name = "bands")]
[Authorize]

public class BandsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BandsController(IMediator mediator)
    {
        _mediator = mediator;
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

    /// <summary>
    /// Get detailed technology-band breakdown for a specific location
    /// Shows separate lists for cellular infrastructure bands and wireless validation bands
    /// Includes summary statistics like generation counts, dominant technology, and modern coverage ratio
    /// </summary>
    /// <param name="locationId">The ID of the location (region, province, municipality, or barangay)</param>
    /// <param name="locationType">The type of location: barangay, municipality, province, region</param>
    /// <returns>Detailed technology-band breakdown with cellular, validation, and all bands per technology plus summary statistics</returns>
    [HttpGet("detailed-technology-breakdown")]
    public async Task<ActionResult<DetailedTechnologyBandBreakdownDto>> GetDetailedTechnologyBandsByLocation(
        [FromQuery] string locationId,
        [FromQuery] string locationType = "municipality")
    {
        var query = new GetTechnologyBandsByLocationQuery(locationId, locationType);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
