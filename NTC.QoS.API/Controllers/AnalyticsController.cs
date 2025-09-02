using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("analytics", Name = "analytics")]
[Authorize]

public class AnalyticsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gets broadband infrastructure information for a specified geographic area.
    /// Returns counts of cells, NAPs, and service provider details within the bounding box.
    /// 
    /// Coordinate constraints:
    /// - Latitude: -5° to 25° (Asia-Pacific region)
    /// - Longitude: 110° to 135° (Asia-Pacific region)
    /// - Maximum area: 10° × 15° (approximately 1,100km × 1,300-1,600km)
    /// 
    /// Validation:
    /// - Bounding box is required and cannot be null
    /// - All coordinates must be within supported geographic bounds
    /// - Min coordinates must be less than max coordinates
    /// - Area size must not exceed maximum limits for performance
    /// </summary>
    /// <param name="boundingBox">Geographic bounding box defining the area of interest</param>
    /// <returns>Broadband infrastructure analysis for the specified area</returns>
    /// <response code="200">Returns the broadband analysis data for the specified area</response>
    /// <response code="400">Invalid bounding box parameters or validation errors</response>
    [HttpPost("area-broadband-info")]
    [RequirePermission(Permissions.CreateAnalytics)]
    [ProducesResponseType(typeof(ApiResponse<AreaBroadbandInfoDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<AreaBroadbandInfoDto>), 400)]
    public async Task<ActionResult<ApiResponse<AreaBroadbandInfoDto>>> GetAreaBroadbandInfo(
        [FromBody, Required] BoundingBoxDto boundingBox)
    {
        var result = await mediator.Send(new GetAreaBroadbandInfoRequest { BoundingBox = boundingBox });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Gets broadband infrastructure information within a specified radius from a center point.
    /// Returns counts of cells, NAPs, and service provider details within the circular area.
    /// 
    /// Coordinate constraints:
    /// - Latitude: -5° to 25° (Asia-Pacific region)
    /// - Longitude: 110° to 135° (Asia-Pacific region)
    /// - Maximum radius: 50,000 meters (50km) for performance
    /// 
    /// Validation:
    /// - Point coordinates are required and must be valid
    /// - All coordinates must be within supported geographic bounds
    /// - Radius must be greater than 0 and not exceed 50km
    /// </summary>
    /// <param name="pointRadius">Center point coordinates and radius in meters</param>
    /// <returns>Broadband infrastructure analysis for the specified circular area</returns>
    /// <response code="200">Returns the broadband analysis data for the specified area</response>
    /// <response code="400">Invalid point/radius parameters or validation errors</response>
    [HttpPost("point-broadband-info")]
    [RequirePermission(Permissions.CreateAnalytics)]
    [ProducesResponseType(typeof(ApiResponse<AreaBroadbandInfoDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<AreaBroadbandInfoDto>), 400)]
    public async Task<ActionResult<ApiResponse<AreaBroadbandInfoDto>>> GetPointBroadbandInfo(
        [FromBody, Required] PointRadiusDto pointRadius)
    {
        var result = await mediator.Send(new GetPointBroadbandInfoRequest { PointRadius = pointRadius });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Gets the supported coordinate bounds and limits for the area broadband info endpoint.
    /// Useful for frontend applications to validate input before making requests.
    /// </summary>
    /// <returns>Geographic constraints and limits</returns>
    /// <response code="200">Returns the geographic constraints and conversion factors</response>
    [HttpGet("geographic-constraints")]
    [RequirePermission(Permissions.ViewAnalytics)]
    [ProducesResponseType(typeof(ApiResponse<GeographicConstraintsDto>), 200)]
    public ActionResult<ApiResponse<GeographicConstraintsDto>> GetGeographicConstraints()
    {
        var constraints = new GeographicConstraintsDto
        {
            SupportedRegion = new BoundingBoxDto
            {
                MinLatitude = GeoSpatialConstants.MinSupportedLatitude,
                MaxLatitude = GeoSpatialConstants.MaxSupportedLatitude,
                MinLongitude = GeoSpatialConstants.MinSupportedLongitude,
                MaxLongitude = GeoSpatialConstants.MaxSupportedLongitude
            },
            MaxAreaSize = new AreaSizeLimitsDto
            {
                MaxLatitudeDifference = GeoSpatialConstants.MaxLatitudeDifference,
                MaxLongitudeDifference = GeoSpatialConstants.MaxLongitudeDifference
            },
            ConversionFactors = new CoordinateConversionDto
            {
                KilometersPerDegreeLatitude = GeoSpatialConstants.KilometersPerDegreeLatitude,
                DegreesToRadians = GeoSpatialConstants.DegreesToRadians
            }
        };

        return Ok(new ApiResponse<GeographicConstraintsDto>
        {
            Succeeded = true,
            Message = "Geographic constraints retrieved successfully.",
            Data = constraints
        });
    }
}
