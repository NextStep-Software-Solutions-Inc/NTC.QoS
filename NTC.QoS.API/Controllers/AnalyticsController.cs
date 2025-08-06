using MediatR;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("analytics", Name = "analytics")]
public class AnalyticsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gets broadband infrastructure information for a specified geographic area.
    /// Returns counts of cells, NAPs, and service provider details within the bounding box.
    /// 
    /// Coordinate constraints:
    /// - Latitude: {GeoSpatialConstants.MinSupportedLatitude}° to {GeoSpatialConstants.MaxSupportedLatitude}°
    /// - Longitude: {GeoSpatialConstants.MinSupportedLongitude}° to {GeoSpatialConstants.MaxSupportedLongitude}°
    /// - Maximum area: {GeoSpatialConstants.MaxLatitudeDifference}° × {GeoSpatialConstants.MaxLongitudeDifference}°
    /// </summary>
    /// <param name="boundingBox">Geographic bounding box defining the area of interest</param>
    /// <returns>Broadband infrastructure analysis for the specified area</returns>
    [HttpPost("area-broadband-info")]
    [ProducesResponseType(typeof(ApiResponse<AreaBroadbandInfoDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<AreaBroadbandInfoDto>), 400)]
    public async Task<ActionResult<ApiResponse<AreaBroadbandInfoDto>>> GetAreaBroadbandInfo([FromBody] BoundingBoxDto boundingBox)
    {
        var result = await mediator.Send(new GetAreaBroadbandInfoRequest { BoundingBox = boundingBox });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Gets the supported coordinate bounds and limits for the area broadband info endpoint.
    /// Useful for frontend applications to validate input before making requests.
    /// </summary>
    /// <returns>Geographic constraints and limits</returns>
    [HttpGet("geographic-constraints")]
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

/// <summary>
/// Geographic constraints and limits for area-based queries.
/// </summary>
public class GeographicConstraintsDto
{
    public BoundingBoxDto SupportedRegion { get; set; } = new();
    public AreaSizeLimitsDto MaxAreaSize { get; set; } = new();
    public CoordinateConversionDto ConversionFactors { get; set; } = new();
}

/// <summary>
/// Maximum area size limits for queries.
/// </summary>
public class AreaSizeLimitsDto
{
    public double MaxLatitudeDifference { get; set; }
    public double MaxLongitudeDifference { get; set; }
}

/// <summary>
/// Coordinate conversion factors for geographic calculations.
/// </summary>
public class CoordinateConversionDto
{
    public double KilometersPerDegreeLatitude { get; set; }
    public double DegreesToRadians { get; set; }
}
