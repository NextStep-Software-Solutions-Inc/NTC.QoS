using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Band.Requests;
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
    /// Get technology-band breakdown for a specific location
    /// </summary>
    /// <param name="locationId">The ID of the location (region, province, municipality, or barangay)</param>
    /// <param name="locationType">The type of location (1=Barangay, 2=Municipality, 3=Provincial, 4=Regional)</param>
    /// <param name="startDate">Optional start date filter for wireless validations</param>
    /// <param name="endDate">Optional end date filter for wireless validations</param>
    /// <returns>Technology-band breakdown showing available bands for each technology</returns>
    [HttpGet("technology-breakdown")]
    public async Task<ActionResult<ApiResponse<TechnologyBandBreakdownDto>>> GetTechnologyBandBreakdownByLocation(
        [FromQuery] Guid locationId,
        [FromQuery] LocationTypeEnum locationType = LocationTypeEnum.Municipality,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var request = new GetTechnologyBandBreakdownByLocationRequest
        {
            LocationId = locationId,
            LocationType = locationType,
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _mediator.Send(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
