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
    [RequirePermission(Permissions.ViewAnalytics)]
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
    [RequirePermission(Permissions.ViewAnalytics)]
    [ProducesResponseType(typeof(ApiResponse<AreaBroadbandInfoDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<AreaBroadbandInfoDto>), 400)]
    public async Task<ActionResult<ApiResponse<AreaBroadbandInfoDto>>> GetPointBroadbandInfo(
        [FromBody, Required] PointRadiusDto pointRadius)
    {
        var result = await mediator.Send(new GetPointBroadbandInfoRequest { PointRadius = pointRadius });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Gets connectivity analysis for specific locations based on CMTS Broadband Mapping Rubric.
    /// Analyzes served/underserved/unserved status for barangays, municipalities, provinces, or regions.
    /// 
    /// CMTS Classification:
    /// - Served (11-15 points): Fully developed, meets most technical and social benchmarks
    /// - Underserved (6-10 points): Basic infrastructure but lacks quality, affordability, or consistency
    /// - Unserved (≤5 points): Virtually no broadband access or supporting infrastructure
    /// 
    /// Analysis includes:
    /// - Fiber coverage assessment
    /// - Internet access speed evaluation  
    /// - Technology type analysis
    /// - Quality of service metrics
    /// - Overall coverage availability
    /// </summary>
    /// <param name="request">Connectivity analysis request parameters</param>
    /// <returns>Detailed connectivity analysis with CMTS scores and infrastructure data</returns>
    /// <response code="200">Returns connectivity analysis data for the specified locations</response>
    /// <response code="400">Invalid request parameters or validation errors</response>
    [HttpPost("connectivity-analysis")]
    [ProducesResponseType(typeof(ApiResponse<ConnectivityAnalysisResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ConnectivityAnalysisResponseDto>), 400)]
    public async Task<ActionResult<ApiResponse<ConnectivityAnalysisResponseDto>>> GetConnectivityAnalysis(
        [FromBody, Required] ConnectivityAnalysisRequestDto request)
    {
        var result = await mediator.Send(new GetConnectivityAnalysisRequest
        {
            LocationIds = request.LocationIds,
            LocationType = request.LocationType,
            FilterByStatus = request.FilterByStatus,
            IncludeSpeedTestData = request.IncludeSpeedTestData,
            IncludeInfrastructureData = request.IncludeInfrastructureData
        });
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Generates or updates connectivity assessments for specific locations.
    /// Calculates CMTS scores based on current infrastructure and speed test data.
    /// 
    /// Assessment Process:
    /// 1. Analyzes infrastructure within location boundaries (cells, NAPs, WiFi hotspots)
    /// 2. Calculates speed test statistics from available data
    /// 3. Applies CMTS rubric scoring (0-3 points per criterion)
    /// 4. Determines overall connectivity status (Served/Underserved/Unserved)
    /// 5. Stores assessment for future reference
    /// 
    /// Use ForceRecalculate=true to regenerate existing assessments.
    /// </summary>
    /// <param name="request">Assessment generation request parameters</param>
    /// <returns>Generated connectivity assessments with detailed scoring</returns>
    /// <response code="200">Returns generated connectivity assessments</response>
    /// <response code="400">Invalid request parameters or validation errors</response>
    [HttpPost("generate-connectivity-assessment")]
    [ProducesResponseType(typeof(ApiResponse<List<LocationConnectivityDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<List<LocationConnectivityDto>>), 400)]
    public async Task<ActionResult<ApiResponse<List<LocationConnectivityDto>>>> GenerateConnectivityAssessment(
        [FromBody, Required] GenerateConnectivityAssessmentRequest request)
    { 
        var result = await mediator.Send(request);
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

    /// <summary>
    /// Gets connectivity status definitions and scoring criteria based on CMTS rubric.
    /// Useful for understanding the assessment methodology and score interpretations.
    /// </summary>
    /// <returns>CMTS rubric definitions and scoring guidelines</returns>
    /// <response code="200">Returns CMTS rubric information</response>
    [HttpGet("connectivity-rubric")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public ActionResult<ApiResponse<object>> GetConnectivityRubric()
    {
        var rubric = new
        {
            ConnectivityStatuses = new[]
            {
                new { 
                    Status = "Served", 
                    ScoreRange = "11-15 points",
                    Description = "Fully developed, meets most technical and social benchmarks",
                    Characteristics = new[]
                    {
                        "Stable internet with speeds meeting or exceeding national broadband standards (e.g., 25 Mbps or higher)",
                        "Fiber optic, LTE/5G, or high-speed DSL infrastructure is in place and active",
                        "High quality (low latency & signal strength), sufficient for online learning, remote work, and streaming",
                        "100% of households, institutions, and establishments within the barangay is covered and have access to fixed or mobile broadband"
                    }
                },
                new { 
                    Status = "Underserved", 
                    ScoreRange = "6-10 points",
                    Description = "Has basic infrastructure or partial access, but lacks quality, affordability, or consistency",
                    Characteristics = new[]
                    {
                        "Internet is available but often slow, unreliable, or below national broadband standards (e.g., <25 Mbps)",
                        "Connectivity provided through legacy technologies (e.g., 2G/3G, dial-up, or outdated satellite)",
                        "Service quality is fair but may suffer from congestion, poor customer support, or usage caps that hinder consistent productivity",
                        "Only 40% to 99% of the population has access, often clustered in urban centers or specific areas"
                    }
                },
                new { 
                    Status = "Unserved", 
                    ScoreRange = "<= 5 points",
                    Description = "Virtually no broadband access or supporting infrastructure",
                    Characteristics = new[]
                    {
                        "No internet access or only minimal access through mobile signal with no broadband infrastructure",
                        "No installed broadband technology, only radio signals or occasional mobile access at best",
                        "Quality is poor to nonexistent, with frequent disconnections or high latency, making it unsuitable for essential tasks",
                        "Less than 40% have access, infrastructure (towers, cables, etc.) is sparse or non-existent"
                    }
                }
            },
            Criteria = new[]
            {
                new {
                    Name = "Fiber Coverage",
                    Weight = "0-3 points",
                    Served = "Fiber optic cables and FTTH NAP facilities for all service provider present w/in the barangay",
                    Underserved = "Few major Telco or consists only of FTTH NAPs from local or different VAS Providers (Others) within the brgy",
                    Unserved = "No installed or present FTTH facilities visible within the barangay"
                },
                new {
                    Name = "Internet Access (Speedtest)",
                    Weight = "0-3 points", 
                    Served = "Stable internet with speeds meeting or exceeding national broadband standards (e.g., 25 Mbps or higher)",
                    Underserved = "Internet is available but often slow, unreliable, or below national broadband standards (e.g., <25 Mbps)",
                    Unserved = "No internet access or only minimal access through mobile signal with no broadband infrastructure"
                },
                new {
                    Name = "Technology Type (Wireless Services)",
                    Weight = "0-3 points",
                    Served = "Fiber optic, LTE/5G, or high-speed DSL infrastructure is in place and active",
                    Underserved = "Connectivity provided through legacy technologies (e.g., 2G/3G, dial-up, or outdated satellite)",
                    Unserved = "No installed broadband technology, only radio signals or occasional mobile access at best"
                },
                new {
                    Name = "Quality of Service",
                    Weight = "0-3 points",
                    Served = "High quality (low latency & signal strength), sufficient for online learning, remote work, and streaming. Good RSSI (40-70 dBm), latency ?30ms, consistent speeds ?50 Mbps",
                    Underserved = "Service quality is fair but may suffer from congestion, poor customer support, or usage caps that hinder consistent productivity. Average/Weak RSSI (70-90 dBm), some speed/latency issues",
                    Unserved = "Quality is poor to nonexistent, with frequent disconnections or high latency, making it unsuitable for essential tasks. Very Weak/NBC RSSI (90+ dBm), latency >100ms"
                },
                new {
                    Name = "Overall Coverage Availability (Radioplanner coverage)",
                    Weight = "0-3 points",
                    Served = "100% of households, institutions, and establishments within the barangay is covered and have access to fixed or mobile broadband",
                    Underserved = "Only 40% to 99% of the population has access, often clustered in urban centers or specific areas",
                    Unserved = "Less than 40% have access, infrastructure (towers, cables, etc.) is sparse or non-existent"
                }
            },
            ScoringGuide = new
            {
                ServedArea = "Total score = 11-15 points - Fully developed, meets most technical and social benchmarks",
                UnderservedArea = "Total score = 6-10 points - Has basic infrastructure or partial access, but lacks quality, affordability, or consistency",
                UnservedArea = "Total score <= 5 points - Virtually no broadband access or supporting infrastructure"
            }
        };

        return Ok(new ApiResponse<object>
        {
            Succeeded = true,
            Message = "CMTS Broadband Mapping Rubric retrieved successfully.",
            Data = rubric
        });
    }
}
