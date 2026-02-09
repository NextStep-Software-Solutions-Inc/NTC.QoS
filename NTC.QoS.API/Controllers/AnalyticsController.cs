using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
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
    [ProducesResponseType(typeof(ApiResponse<ConnectivityRubricDto>), 200)]
    public ActionResult<ApiResponse<ConnectivityRubricDto>> GetConnectivityRubric()
    {
        var rubric = new ConnectivityRubricDto
        {
            ConnectivityStatuses = new List<ConnectivityStatusDto>
            {
                new ConnectivityStatusDto
                {
                    Status = "Served",
                    ScoreRange = "11-15 points",
                    Description = "Fully developed, meets most technical and social benchmarks",
                    Characteristics = new List<string>
                    {
                        "Stable internet with speeds meeting or exceeding national broadband standards (e.g., 25 Mbps or higher)",
                        "Fiber optic, LTE/5G, or high-speed DSL infrastructure is in place and active",
                        "High quality (low latency & signal strength), sufficient for online learning, remote work, and streaming",
                        "100% of households, institutions, and establishments within the barangay is covered and have access to fixed or mobile broadband"
                    }
                },
                new ConnectivityStatusDto
                {
                    Status = "Underserved",
                    ScoreRange = "6-10 points",
                    Description = "Has basic infrastructure or partial access, but lacks quality, affordability, or consistency",
                    Characteristics = new List<string>
                    {
                        "Internet is available but often slow, unreliable, or below national broadband standards (e.g., <25 Mbps)",
                        "Connectivity provided through legacy technologies (e.g., 2G/3G, dial-up, or outdated satellite)",
                        "Service quality is fair but may suffer from congestion, poor customer support, or usage caps that hinder consistent productivity",
                        "Only 40% to 99% of the population has access, often clustered in urban centers or specific areas"
                    }
                },
                new ConnectivityStatusDto
                {
                    Status = "Unserved",
                    ScoreRange = "<= 5 points",
                    Description = "Virtually no broadband access or supporting infrastructure",
                    Characteristics = new List<string>
                    {
                        "No internet access or only minimal access through mobile signal with no broadband infrastructure",
                        "No installed broadband technology, only radio signals or occasional mobile access at best",
                        "Quality is poor to nonexistent, with frequent disconnections or high latency, making it unsuitable for essential tasks",
                        "Less than 40% have access, infrastructure (towers, cables, etc.) is sparse or non-existent"
                    }
                }
            },
            Criteria = new List<RubricCriterionDto>
            {
                new RubricCriterionDto
                {
                    Name = "Fiber Coverage",
                    Weight = "0-3 points",
                    Served = "Fiber optic cables and FTTH NAP facilities for all service provider present w/in the barangay",
                    Underserved = "Few major Telco or consists only of FTTH NAPs from local or different VAS Providers (Others) within the brgy",
                    Unserved = "No installed or present FTTH facilities visible within the barangay"
                },
                new RubricCriterionDto
                {
                    Name = "Internet Access (Speedtest)",
                    Weight = "0-3 points",
                    Served = "Stable internet with speeds meeting or exceeding national broadband standards (e.g., 25 Mbps or higher)",
                    Underserved = "Internet is available but often slow, unreliable, or below national broadband standards (e.g., <25 Mbps)",
                    Unserved = "No internet access or only minimal access through mobile signal with no broadband infrastructure"
                },
                new RubricCriterionDto
                {
                    Name = "Technology Type (Wireless Services)",
                    Weight = "0-3 points",
                    Served = "Fiber optic, LTE/5G, or high-speed DSL infrastructure is in place and active",
                    Underserved = "Connectivity provided through legacy technologies (e.g., 2G/3G, dial-up, or outdated satellite)",
                    Unserved = "No installed broadband technology, only radio signals or occasional mobile access at best"
                },
                new RubricCriterionDto
                {
                    Name = "Quality of Service",
                    Weight = "0-3 points",
                    Served = "High quality (low latency & signal strength), sufficient for online learning, remote work, and streaming. Good RSSI (40-70 dBm), latency ?30ms, consistent speeds ?50 Mbps",
                    Underserved = "Service quality is fair but may suffer from congestion, poor customer support, or usage caps that hinder consistent productivity. Average/Weak RSSI (70-90 dBm), some speed/latency issues",
                    Unserved = "Quality is poor to nonexistent, with frequent disconnections or high latency, making it unsuitable for essential tasks. Very Weak/NBC RSSI (90+ dBm), latency >100ms"
                },
                new RubricCriterionDto
                {
                    Name = "Overall Coverage Availability (Radioplanner coverage)",
                    Weight = "0-3 points",
                    Served = "100% of households, institutions, and establishments within the barangay is covered and have access to fixed or mobile broadband",
                    Underserved = "Only 40% to 99% of the population has access, often clustered in urban centers or specific areas",
                    Unserved = "Less than 40% have access, infrastructure (towers, cables, etc.) is sparse or non-existent"
                }
            },
            ScoringGuide = new ScoringGuideDto
            {
                ServedArea = "Total score = 11-15 points - Fully developed, meets most technical and social benchmarks",
                UnderservedArea = "Total score = 6-10 points - Has basic infrastructure or partial access, but lacks quality, affordability, or consistency",
                UnservedArea = "Total score <= 5 points - Virtually no broadband access or supporting infrastructure"
            }
        };

        return Ok(new ApiResponse<ConnectivityRubricDto>
        {
            Succeeded = true,
            Message = "CMTS Broadband Mapping Rubric retrieved successfully.",
            Data = rubric
        });
    }

    /// <summary>
    /// Start a background job to generate connectivity assessments
    /// Use this for large-scale processing that would timeout in regular API calls
    /// 
    /// Location Specification:
    /// - RegionIds: Process assessments for specified regions
    /// - ProvinceIds: Process assessments for specified provinces
    /// - MunicipalityIds: Process assessments for specified municipalities
    /// - BarangayIds: Process assessments for specified barangays
    /// 
    /// At least one location type must be provided. Multiple filters can be specified,
    /// but the most specific filter takes precedence (Barangay > Municipality > Province > Region).
    /// The job will process assessments for the specified locations and optionally their children 
    /// (if GenerateHierarchicalAssessments is true).
    /// </summary>
    /// <param name="request">Background job request parameters including typed location IDs</param>
    /// <returns>Job ID and tracking information</returns>
    [HttpPost("connectivity-assessment/background")]
    [ProducesResponseType(typeof(ApiResponse<ConnectivityAssessmentJobResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConnectivityAssessmentJobResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ConnectivityAssessmentJobResponseDto>>> StartConnectivityAssessmentJob(
        [FromBody] StartConnectivityAssessmentJobRequest request)
    {
        var response = await mediator.Send(request);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Get the status of a background connectivity assessment job
    /// Includes database verification for completed jobs to ensure data integrity
    /// </summary>
    /// <param name="jobId">Job identifier returned from the start job endpoint</param>
    /// <param name="forceVerification">Force database verification even if already verified (default: false)</param>
    /// <returns>Current job status and progress information with database verification</returns>
    [HttpGet("connectivity-assessment/job/{jobId}/status")]
    [ProducesResponseType(typeof(ApiResponse<ConnectivityAssessmentJobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConnectivityAssessmentJobStatusDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConnectivityAssessmentJobStatusDto>>> GetConnectivityAssessmentJobStatus(
        [FromRoute] string jobId,
        [FromQuery] bool forceVerification = false)
    {
        var request = new GetConnectivityAssessmentJobStatusRequest 
        { 
            JobId = jobId,
            ForceVerification = forceVerification
        };
        var response = await mediator.Send(request);
        return response.Succeeded ? Ok(response) : NotFound(response);
    }

    /// <summary>
    /// Gets NAP analytics for dashboard visualization.
    /// Provides market share distribution, provider comparison, geographic coverage, and market analysis.
    /// 
    /// Includes:
    /// - Total NAPs count and active providers
    /// - Average NAPs per provider
    /// - NAP density per square kilometer
    /// - Market share distribution by provider
    /// - Provider comparison data
    /// - Leading provider information
    /// - Geographic coverage bounds
    /// - Market analysis (concentration, competition score, coverage quality)
    /// 
    /// Filters:
    /// - Geographic: Barangay, Municipality, Province, or Region IDs
    /// - Service Provider IDs
    /// </summary>
    /// <param name="barangayIds">Optional filter by barangay IDs</param>
    /// <param name="municipalityIds">Optional filter by municipality IDs</param>
    /// <param name="provinceIds">Optional filter by province IDs</param>
    /// <param name="regionIds">Optional filter by region IDs</param>
    /// <param name="serviceProviderIds">Optional filter by service provider IDs</param>
    /// <param name="includeGeographicCoverage">Include geographic coverage calculation (default: true)</param>
    /// <returns>NAP analytics data for dashboard</returns>
    /// <response code="200">Returns NAP analytics data</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpGet("nap-analytics")]
    [RequirePermission(Permissions.ViewAnalytics)]
    [ProducesResponseType(typeof(ApiResponse<NAPAnalyticsResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<NAPAnalyticsResponseDto>), 400)]
    public async Task<ActionResult<ApiResponse<NAPAnalyticsResponseDto>>> GetNAPAnalytics(
        [FromQuery] List<Guid>? barangayIds = null,
        [FromQuery] List<Guid>? municipalityIds = null,
        [FromQuery] List<Guid>? provinceIds = null,
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] List<Guid>? serviceProviderIds = null,
        [FromQuery] bool includeGeographicCoverage = true)
    {
        var request = new GetNAPAnalyticsRequest
        {
            BarangayIds = barangayIds,
            MunicipalityIds = municipalityIds,
            ProvinceIds = provinceIds,
            RegionIds = regionIds,
            ServiceProviderIds = serviceProviderIds,
            IncludeGeographicCoverage = includeGeographicCoverage
        };

        var result = await mediator.Send(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Gets speedtest analytics for dashboard visualization.
    /// Provides comprehensive performance metrics, provider comparison, and signal quality analysis.
    /// 
    /// Includes:
    /// - Overview statistics (total tests, average/median speeds, ping, provider count)
    /// - Provider performance comparison (download/upload speeds, test counts)
    /// - Signal quality distribution (excellent, good, average, weak, very weak)
    /// - Provider rankings with performance scores and badges
    /// - Performance ratings and competition analysis
    /// - Coverage area estimation
    /// 
    /// Note: Calculates ALL statistics on backend regardless of pagination.
    /// Uses median for more representative metrics (less affected by outliers).
    /// 
    /// Filters:
    /// - Geographic: Barangay, Municipality, Province, or Region IDs
    /// - Date Range: Optional start and end dates for temporal filtering
    /// </summary>
    /// <param name="barangayIds">Optional filter by barangay IDs</param>
    /// <param name="municipalityIds">Optional filter by municipality IDs</param>
    /// <param name="provinceIds">Optional filter by province IDs</param>
    /// <param name="regionIds">Optional filter by region IDs</param>
    /// <param name="startDate">Optional start date for filtering speedtests</param>
    /// <param name="endDate">Optional end date for filtering speedtests</param>
    /// <returns>Speedtest analytics data for dashboard</returns>
    /// <response code="200">Returns speedtest analytics data</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpGet("speedtest-analytics")]
    [RequirePermission(Permissions.ViewAnalytics)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false,
        VaryByQueryKeys = new[] { "barangayIds", "municipalityIds", "provinceIds", "regionIds", "startDate", "endDate" })]
    [ProducesResponseType(typeof(ApiResponse<SpeedtestAnalyticsDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SpeedtestAnalyticsDto>), 400)]
    public async Task<ActionResult<ApiResponse<SpeedtestAnalyticsDto>>> GetSpeedtestAnalytics(
        [FromQuery] List<Guid>? barangayIds = null,
        [FromQuery] List<Guid>? municipalityIds = null,
        [FromQuery] List<Guid>? provinceIds = null,
        [FromQuery] List<Guid>? regionIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var request = new GetSpeedtestAnalyticsRequest 
        { 
            BarangayIds = barangayIds,
            MunicipalityIds = municipalityIds,
            ProvinceIds = provinceIds,
            RegionIds = regionIds,
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await mediator.Send(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
