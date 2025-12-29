using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// API endpoints for managing Barangay broadband coverage data
/// </summary>
[ApiController]
[Route("barangay-coverages", Name = "barangay-coverages")]
[Authorize]
public class BarangayCoverageController : ControllerBase
{
    private readonly IMediator _mediator;

    public BarangayCoverageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create coverage data for a specific barangay
    /// </summary>
    /// <param name="coverageDto">Coverage data to create</param>
    /// <returns>Created coverage data with computed fields</returns>
    [HttpPost]
    [RequirePermission(Permissions.CreateBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BarangayCoverageDto>>> CreateCoverage(CreateBarangayCoverageDto coverageDto)
    {
        var result = await _mediator.Send(new CreateBarangayCoverageRequest
        {
            Coverage = coverageDto
        });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Update coverage data for a specific barangay
    /// </summary>
    /// <param name="barangayId">The ID of the barangay</param>
    /// <param name="coverageDto">Updated coverage data</param>
    /// <returns>Updated coverage data with computed fields</returns>
    [HttpPatch("barangays/{barangayId}")]
    [RequirePermission(Permissions.UpdateBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BarangayCoverageDto>>> UpdateCoverageByBarangayId(Guid barangayId, UpdateBarangayCoverageDto coverageDto)
    {
        var result = await _mediator.Send(new UpdateBarangayCoverageRequest
        {
            BarangayId = barangayId,
            Coverage = coverageDto
        });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get coverage data for a specific barangay
    /// </summary>
    /// <param name="barangayId">The ID of the barangay</param>
    /// <returns>Coverage data with computed fields</returns>
    [HttpGet("barangays/{barangayId}")]
    [RequirePermission(Permissions.ViewBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BarangayCoverageDto>>> GetCoverageByBarangayId(Guid barangayId)
    {
        var result = await _mediator.Send(new GetBarangayCoverageByBarangayIdRequest
        {
            BarangayId = barangayId
        });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Delete coverage data for a specific barangay
    /// </summary>
    /// <param name="barangayId">The ID of the barangay</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("barangays/{barangayId}")]
    [RequirePermission(Permissions.DeleteBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCoverageByBarangayId(Guid barangayId)
    {
        var result = await _mediator.Send(new DeleteBarangayCoverageByBarangayIdRequest
        {
            BarangayId = barangayId
        });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get all coverage data for barangays in a specific region
    /// </summary>
    /// <param name="regionId">The ID of the region</param>
    /// <returns>List of coverage data for all barangays in the region</returns>
    [HttpGet("regions/{regionId}")]
    [RequirePermission(Permissions.ViewBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<List<BarangayCoverageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BarangayCoverageDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<BarangayCoverageDto>>>> GetCoverageByRegion(Guid regionId)
    {
        var result = await _mediator.Send(new GetBarangayCoverageByRegionRequest
        {
            RegionId = regionId
        });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
