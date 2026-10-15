using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Services.MCP;

/// <summary>
/// MCP (Model Context Protocol) Server Service implementation.
/// Provides tools that allow AI models to query the database through existing API endpoints.
/// </summary>
public class MCPServerService : IMCPServerService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IMediator _mediator;
    private readonly ILogger<MCPServerService> _logger;

    public MCPServerService(IMediator mediator, ILogger<MCPServerService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public Task<string> GetToolsAsync()
    {
        var tools = new object[]
        {
            new
            {
                name = "get_providers",
                description = "Get a paginated list of telecommunications service providers. Returns provider information including names, services, and basic details.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        pageSize = new { type = "integer", description = "Number of results per page (default: 10)" },
                        pageNumber = new { type = "integer", description = "Page number to retrieve (default: 1)" },
                        serviceIds = new { type = "array", items = new { type = "string" }, description = "Optional array of service IDs to filter by" },
                        names = new { type = "array", items = new { type = "string" }, description = "Optional array of provider names to filter by" }
                    }
                }
            },
            new
            {
                name = "get_connectivity_analysis",
                description = "Get detailed connectivity analysis for specific locations based on CMTS Broadband Mapping Rubric. Analyzes served/underserved/unserved status for barangays, municipalities, provinces, or regions. Returns infrastructure data, speed test statistics, and CMTS scores.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        locationIds = new { type = "array", items = new { type = "string" }, description = "Array of location GUIDs to analyze" },
                        locationType = new { type = "string", description = "Type of location: Barangay, Municipality, Province, or Region" },
                        filterByStatus = new { type = "string", description = "Optional filter by connectivity status: Served, Underserved, or Unserved" },
                        includeSpeedTestData = new { type = "boolean", description = "Include speed test data in the response (default: false)" },
                        includeInfrastructureData = new { type = "boolean", description = "Include infrastructure details in the response (default: false)" }
                    },
                    required = new[] { "locationIds", "locationType" }
                }
            },
            new
            {
                name = "get_area_broadband_info",
                description = "Get broadband infrastructure information for a geographic bounding box. Returns counts of cells, NAPs, and service provider details within the specified area.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        minLatitude = new { type = "number", description = "Minimum latitude of bounding box (-5 to 25)" },
                        minLongitude = new { type = "number", description = "Minimum longitude of bounding box (110 to 135)" },
                        maxLatitude = new { type = "number", description = "Maximum latitude of bounding box (-5 to 25)" },
                        maxLongitude = new { type = "number", description = "Maximum longitude of bounding box (110 to 135)" }
                    },
                    required = new[] { "minLatitude", "minLongitude", "maxLatitude", "maxLongitude" }
                }
            },
            new
            {
                name = "get_point_broadband_info",
                description = "Get broadband infrastructure information within a radius from a center point. Returns counts of cells, NAPs, and service provider details within the circular area.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        latitude = new { type = "number", description = "Center point latitude (-5 to 25)" },
                        longitude = new { type = "number", description = "Center point longitude (110 to 135)" },
                        radiusMeters = new { type = "number", description = "Radius in meters (max 50000)" }
                    },
                    required = new[] { "latitude", "longitude", "radiusMeters" }
                }
            },
            new
            {
                name = "generate_connectivity_assessment",
                description = "Generate or update connectivity assessments for specific locations. Calculates CMTS scores based on current infrastructure and speed test data. Use this to get fresh connectivity status for locations.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        locationIds = new { type = "array", items = new { type = "string" }, description = "Array of location GUIDs to assess" },
                        locationType = new { type = "string", description = "Type of location: Barangay, Municipality, Province, or Region" },
                        forceRecalculate = new { type = "boolean", description = "Force recalculation even if assessment exists (default: false)" }
                    },
                    required = new[] { "locationIds", "locationType" }
                }
            }
        };

        var toolsJson = JsonSerializer.Serialize(tools, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(toolsJson);
    }

    public async Task<string> ExecuteToolAsync(string toolName, string parameters)
    {
        try
        {
            _logger.LogInformation("Executing MCP tool: {ToolName} with parameters: {Parameters}", toolName, parameters);

            object? result = toolName switch
            {
                "get_providers" => await ExecuteGetProvidersAsync(parameters),
                "get_connectivity_analysis" => await ExecuteGetConnectivityAnalysisAsync(parameters),
                "get_area_broadband_info" => await ExecuteGetAreaBroadbandInfoAsync(parameters),
                "get_point_broadband_info" => await ExecuteGetPointBroadbandInfoAsync(parameters),
                "generate_connectivity_assessment" => await ExecuteGenerateConnectivityAssessmentAsync(parameters),
                _ => throw new ArgumentException($"Unknown tool: {toolName}")
            };

            var resultJson = JsonSerializer.Serialize(result, _jsonOptions);
            _logger.LogInformation("MCP tool {ToolName} executed successfully", toolName);
            return resultJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MCP tool: {ToolName}", toolName);
            var error = new { error = ex.Message, toolName };
            return JsonSerializer.Serialize(error, _jsonOptions);
        }
    }

    private async Task<object?> ExecuteGetProvidersAsync(string parameters)
    {
        var paramsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parameters, _jsonOptions);

        var pageSize = paramsDict?.TryGetValue("pageSize", out var ps) == true ? ps.GetInt32() : 10;
        var pageNumber = paramsDict?.TryGetValue("pageNumber", out var pn) == true ? pn.GetInt32() : 1;

        List<Guid>? serviceIds = null;
        if (paramsDict?.TryGetValue("serviceIds", out var sids) == true && sids.ValueKind == JsonValueKind.Array)
        {
            serviceIds = sids.EnumerateArray()
                .Select(e => Guid.Parse(e.GetString()!))
                .ToList();
        }

        List<string>? names = null;
        if (paramsDict?.TryGetValue("names", out var ns) == true && ns.ValueKind == JsonValueKind.Array)
        {
            names = ns.EnumerateArray()
                .Select(e => e.GetString()!)
                .ToList();
        }

        var request = new GetAllProvidersRequest(pageSize, pageNumber, serviceIds, names, includeLogoData: false);
        var response = await _mediator.Send(request);

        return response;
    }

    private async Task<object?> ExecuteGetConnectivityAnalysisAsync(string parameters)
    {
        var requestDto = JsonSerializer.Deserialize<ConnectivityAnalysisRequestDto>(parameters, _jsonOptions);

        if (requestDto == null)
            throw new ArgumentException("Invalid parameters for connectivity analysis");

        var request = new GetConnectivityAnalysisRequest
        {
            LocationIds = requestDto.LocationIds,
            LocationType = requestDto.LocationType,
            FilterByStatus = requestDto.FilterByStatus,
            IncludeSpeedTestData = requestDto.IncludeSpeedTestData,
            IncludeInfrastructureData = requestDto.IncludeInfrastructureData
        };

        var response = await _mediator.Send(request);
        return response;
    }

    private async Task<object?> ExecuteGetAreaBroadbandInfoAsync(string parameters)
    {
        var paramsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parameters, _jsonOptions);

        if (paramsDict == null)
            throw new ArgumentException("Invalid parameters for area broadband info");

        var boundingBox = new BoundingBoxDto
        {
            MinLatitude = paramsDict["minLatitude"].GetDouble(),
            MinLongitude = paramsDict["minLongitude"].GetDouble(),
            MaxLatitude = paramsDict["maxLatitude"].GetDouble(),
            MaxLongitude = paramsDict["maxLongitude"].GetDouble()
        };

        var request = new GetAreaBroadbandInfoRequest { BoundingBox = boundingBox };
        var response = await _mediator.Send(request);
        return response;
    }

    private async Task<object?> ExecuteGetPointBroadbandInfoAsync(string parameters)
    {
        var paramsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parameters, _jsonOptions);

        if (paramsDict == null)
            throw new ArgumentException("Invalid parameters for point broadband info");

        var pointRadius = new PointRadiusDto
        {
            Latitude = paramsDict["latitude"].GetDouble(),
            Longitude = paramsDict["longitude"].GetDouble(),
            RadiusMeters = paramsDict["radiusMeters"].GetDouble()
        };

        var request = new GetPointBroadbandInfoRequest { PointRadius = pointRadius };
        var response = await _mediator.Send(request);
        return response;
    }

    private async Task<object?> ExecuteGenerateConnectivityAssessmentAsync(string parameters)
    {
        var paramsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parameters, _jsonOptions);

        if (paramsDict == null)
            throw new ArgumentException("Invalid parameters for connectivity assessment");

        var locationIds = paramsDict["locationIds"].EnumerateArray()
            .Select(e => Guid.Parse(e.GetString()!))
            .ToList();

        var locationType = Enum.Parse<Domain.Enums.LocationTypeEnum>(
            paramsDict["locationType"].GetString()!, ignoreCase: true);

        var forceRecalculate = paramsDict.TryGetValue("forceRecalculate", out var fr) && fr.GetBoolean();

        var request = new GenerateConnectivityAssessmentRequest
        {
            LocationIds = locationIds,
            LocationType = locationType,
            ForceRecalculate = forceRecalculate
        };

        var response = await _mediator.Send(request);
        return response;
    }
}
