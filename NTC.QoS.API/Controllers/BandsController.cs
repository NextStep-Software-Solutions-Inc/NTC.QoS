using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Application.Features.Band.Queries.GetTechnologyBandsByLocation;
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
    [RequirePermission(Permissions.CreateBands)]
    public async Task<IActionResult> Post(CreateBandDto bandDto)
    {
        var band = await _mediator.Send(new CreateBandRequest
        {
            Band = bandDto
        });

        return band.Succeeded ? Ok(band) : BadRequest(band);
    }

    [HttpGet]
    [RequirePermission(Permissions.ViewBands)]
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
    [RequirePermission(Permissions.ViewBands)]
    public async Task<ActionResult<ApiResponse<BandDto>>> GetBandById(Guid id)
    {
        var request = new GetBandByIdRequest(id);
        var band = await _mediator.Send(request);
        return band.Succeeded ? Ok(band) : BadRequest(band);
    }

    [HttpPut("{id}")]
    [RequirePermission(Permissions.UpdateBands)]
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
    [RequirePermission(Permissions.DeleteBands)]
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
    /// <param name="locationType">The type of location: "region", "province", "municipality", or "barangay"</param>
    /// <returns>Detailed technology-band breakdown with cellular, validation, and all bands per technology plus summary statistics</returns>
    [HttpGet("technology-breakdown")]
    public async Task<ActionResult<DetailedTechnologyBandBreakdownDto>> GetTechnologyBandBreakdown(
        [FromQuery] string locationId,
        [FromQuery] string locationType = "municipality")
    {
        // Validate locationType
        var validLocationTypes = new[] { "region", "province", "municipality", "barangay" };
        if (!validLocationTypes.Contains(locationType.ToLower()))
        {
            return BadRequest(new { error = $"Invalid locationType. Must be one of: {string.Join(", ", validLocationTypes)}" });
        }

        var query = new GetTechnologyBandsByLocationQuery(locationId, locationType.ToLower());
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
