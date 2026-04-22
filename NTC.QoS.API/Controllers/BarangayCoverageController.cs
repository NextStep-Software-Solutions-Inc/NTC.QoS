using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;
using NTC.QoS.Domain.Models.RequestParams;

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
    /// <returns>GUID of the created coverage record</returns>
    [HttpPost]
    [RequirePermission(Permissions.CreateBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCoverage(CreateBarangayCoverageDto coverageDto)
    {
        var result = await _mediator.Send(new CreateBarangayCoverageRequest
        {
            Coverage = coverageDto
        });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Update coverage data for a specific coverage record
    /// </summary>
    /// <param name="id">The ID of the coverage record</param>
    /// <param name="coverageDto">Updated coverage data</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id}")]
    [RequirePermission(Permissions.UpdateBarangayCoverage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCoverageById(Guid id, UpdateBarangayCoverageDto coverageDto)
    {
        try
        {
            await _mediator.Send(new UpdateBarangayCoverageByIdRequest
            {
                Id = id,
                Coverage = coverageDto
            });

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all coverage data with optional filters
    /// </summary>
    /// <param name="regionIds">Optional list of region IDs to filter by</param>
    /// <param name="provinceIds">Optional list of province IDs to filter by</param>
    /// <param name="municipalityIds">Optional list of municipality IDs to filter by</param>
    /// <param name="barangayIds">Optional list of barangay IDs to filter by</param>
    /// <returns>List of coverage data (lightweight, without relationships)</returns>
    [HttpGet]
    [RequirePermission(Permissions.ViewBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<List<BarangayCoverageLightDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<BarangayCoverageLightDto>>>> GetAllCoverages(
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] List<Guid>? provinceIds = null,
        [FromQuery] List<Guid>? municipalityIds = null,
        [FromQuery] List<Guid>? barangayIds = null)
    {
        var result = await _mediator.Send(new GetAllBarangayCoveragesRequest
        {
            RegionIds = regionIds,
            ProvinceIds = provinceIds,
            MunicipalityIds = municipalityIds,
            BarangayIds = barangayIds
        });

        return Ok(result);
    }

    /// <summary>
    /// Get coverage data for a specific coverage record
    /// </summary>
    /// <param name="id">The ID of the coverage record</param>
    /// <returns>Coverage data with relationships (excluding boundaries)</returns>
    [HttpGet("{id}")]
    [RequirePermission(Permissions.ViewBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BarangayCoverageDto>>> GetCoverageById(Guid id)
    {
        var result = await _mediator.Send(new GetBarangayCoverageByIdRequest
        {
            Id = id
        });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Delete coverage data for a specific coverage record
    /// </summary>
    /// <param name="id">The ID of the coverage record</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeleteBarangayCoverage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCoverageById(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteBarangayCoverageByIdRequest
            {
                Id = id
            });

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Legacy endpoints kept for backwards compatibility - will be removed in future version
    
    /// <summary>
    /// [DEPRECATED] Update coverage data for a specific barangay. Use PUT /barangay-coverages/{id} instead.
    /// </summary>
    [HttpPatch("barangays/{barangayId}")]
    [RequirePermission(Permissions.UpdateBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(IgnoreApi = true)]
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
    /// [DEPRECATED] Get coverage data for a specific barangay. Use GET /barangay-coverages/{id} instead.
    /// </summary>
    [HttpGet("barangays/{barangayId}")]
    [RequirePermission(Permissions.ViewBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BarangayCoverageDto>), StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<ApiResponse<BarangayCoverageDto>>> GetCoverageByBarangayId(Guid barangayId)
    {
        var result = await _mediator.Send(new GetBarangayCoverageByBarangayIdRequest
        {
            BarangayId = barangayId
        });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// [DEPRECATED] Delete coverage data for a specific barangay. Use DELETE /barangay-coverages/{id} instead.
    /// </summary>
    [HttpDelete("barangays/{barangayId}")]
    [RequirePermission(Permissions.DeleteBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCoverageByBarangayId(Guid barangayId)
    {
        var result = await _mediator.Send(new DeleteBarangayCoverageByBarangayIdRequest
        {
            BarangayId = barangayId
        });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// [DEPRECATED] Get all coverage data for barangays in a specific region. Use GET /barangay-coverages with regionIds query param instead.
    /// </summary>
    [HttpGet("regions/{regionId}")]
    [RequirePermission(Permissions.ViewBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<List<BarangayCoverageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BarangayCoverageDto>>), StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<ApiResponse<List<BarangayCoverageDto>>>> GetCoverageByRegion(Guid regionId)
    {
        var result = await _mediator.Send(new GetBarangayCoverageByRegionRequest
        {
            RegionId = regionId
        });

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    // ============================================================================
    // V2 VIEWPORT-BASED ENDPOINTS (Optimized for map rendering)
    // ============================================================================

    /// <summary>
    /// Get barangay boundary polygons with wired FTTH fiber coverage data within a
    /// map viewport bounding box (v2 API – viewport-optimized).
    ///
    /// Only barangays whose boundaries intersect the viewport AND have
    /// <c>WiredFtthPercentage &gt; 0</c> are returned.
    ///
    /// <para><strong>⚠ Map Rendering Note:</strong>
    /// The returned GeoJSON features represent the <em>Fiber Networks</em> coverage layer.
    /// To prevent them from obscuring place-name labels you MUST add this layer
    /// <strong>before</strong> the first symbol/label layer of the active map style
    /// using the <c>beforeId</c> parameter in MapLibre/Mapbox GL JS:</para>
    /// <code>
    ///   const firstSymbolId = map.getStyle().layers.find(l => l.type === 'symbol')?.id;
    ///   map.addLayer({
    ///     id: 'fiber-coverage-fill',
    ///     type: 'fill',
    ///     source: 'fiber-coverage',   // GeoJSON source built from this response
    ///     paint: { 'fill-color': '#c026d3', 'fill-opacity': 0.35 }
    ///   }, firstSymbolId);            // ← insert BEFORE the first label layer
    /// </code>
    /// <para>This approach is required for all supported map tile styles
    /// (Streets, Topography Dark, Basic) to keep labels legible.</para>
    /// </summary>
    /// <param name="query">Viewport bounding box and optional administrative filters.</param>
    /// <returns>
    /// List of <see cref="FiberCoverageViewportDto"/> items, each containing the barangay
    /// boundary as a GeoJSON Feature and the associated fiber coverage percentage.
    /// </returns>
    [HttpGet("/v2/barangay-coverages/viewport")]
    [RequirePermission(Permissions.ViewBarangayCoverage)]
    [ProducesResponseType(typeof(ApiResponse<List<FiberCoverageViewportDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<FiberCoverageViewportDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<FiberCoverageViewportDto>>>> GetFiberCoverageViewport(
        [FromQuery] ViewportQueryParams query)
    {
        if (!query.IsViewportValid())
        {
            return BadRequest(new ApiResponse<List<FiberCoverageViewportDto>>
            {
                Succeeded = false,
                Message = "Invalid viewport bounds. Ensure minLat < maxLat and minLng < maxLng."
            });
        }

        var request = new GetFiberCoverageViewportRequest
        {
            Viewport = query.ToViewportBounds(),
            RegionIds = query.RegionIds,
            ProvinceIds = query.ProvinceIds,
            MunicipalityIds = query.MunicipalityIds
        };

        var result = await _mediator.Send(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
