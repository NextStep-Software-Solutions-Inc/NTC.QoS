using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Application.Services.MCP;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Provider;
using System.Text.Json;
using Xunit;

namespace NTC.QoS.Tests.Application.Services.MCP;

public class MCPServerServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<MCPServerService>> _loggerMock;
    private readonly MCPServerService _service;

    public MCPServerServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<MCPServerService>>();
        _service = new MCPServerService(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetToolsAsync_ReturnsValidJsonWithAllTools()
    {
        // Act
        var result = await _service.GetToolsAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON
        var tools = JsonSerializer.Deserialize<JsonElement[]>(result);
        tools.Should().NotBeNull();
        tools.Should().HaveCount(5);

        // Verify all expected tools are present
        var toolNames = tools!.Select(t => t.GetProperty("name").GetString()).ToList();
        toolNames.Should().Contain("get_providers");
        toolNames.Should().Contain("get_connectivity_analysis");
        toolNames.Should().Contain("get_area_broadband_info");
        toolNames.Should().Contain("get_point_broadband_info");
        toolNames.Should().Contain("generate_connectivity_assessment");
    }

    [Fact]
    public async Task GetToolsAsync_ToolsHaveRequiredProperties()
    {
        // Act
        var result = await _service.GetToolsAsync();
        var tools = JsonSerializer.Deserialize<JsonElement[]>(result);

        // Assert
        foreach (var tool in tools!)
        {
            tool.TryGetProperty("name", out _).Should().BeTrue();
            tool.TryGetProperty("description", out _).Should().BeTrue();
            tool.TryGetProperty("inputSchema", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task ExecuteToolAsync_GetProviders_CallsMediatorWithCorrectRequest()
    {
        // Arrange
        var parameters = JsonSerializer.Serialize(new
        {
            pageSize = 20,
            pageNumber = 2
        });

        var expectedResponse = new PaginatedResponse<ProviderDto>(
            new List<ProviderDto>(),
            pageNumber: 2,
            pageSize: 20,
            totalRows: 0
        );

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllProvidersRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.ExecuteToolAsync("get_providers", parameters);

        // Assert
        result.Should().NotBeNullOrEmpty();
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetAllProvidersRequest>(r => r.PageSize == 20 && r.PageNumber == 2),
            default), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_GetConnectivityAnalysis_CallsMediatorWithCorrectRequest()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var parameters = JsonSerializer.Serialize(new
        {
            locationIds = new[] { locationId.ToString() },
            locationType = "Municipality",
            includeSpeedTestData = true,
            includeInfrastructureData = true
        });

        var expectedResponse = new ApiResponse<ConnectivityAnalysisResponseDto>
        {
            Succeeded = true,
            Data = new ConnectivityAnalysisResponseDto()
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetConnectivityAnalysisRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.ExecuteToolAsync("get_connectivity_analysis", parameters);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var response = JsonSerializer.Deserialize<JsonElement>(result);
        response.TryGetProperty("succeeded", out var succeeded).Should().BeTrue();
        succeeded.GetBoolean().Should().BeTrue();
        
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetConnectivityAnalysisRequest>(r => 
                r.LocationIds.Contains(locationId) &&
                r.LocationType == LocationTypeEnum.Municipality),
            default), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_GetAreaBroadbandInfo_CallsMediatorWithCorrectRequest()
    {
        // Arrange
        var parameters = JsonSerializer.Serialize(new
        {
            minLatitude = 10.0,
            minLongitude = 120.0,
            maxLatitude = 15.0,
            maxLongitude = 125.0
        });

        var expectedResponse = new ApiResponse<AreaBroadbandInfoDto>
        {
            Succeeded = true,
            Data = new AreaBroadbandInfoDto()
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAreaBroadbandInfoRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.ExecuteToolAsync("get_area_broadband_info", parameters);

        // Assert
        result.Should().NotBeNullOrEmpty();
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetAreaBroadbandInfoRequest>(r =>
                r.BoundingBox.MinLatitude == 10.0 &&
                r.BoundingBox.MinLongitude == 120.0 &&
                r.BoundingBox.MaxLatitude == 15.0 &&
                r.BoundingBox.MaxLongitude == 125.0),
            default), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_GetPointBroadbandInfo_CallsMediatorWithCorrectRequest()
    {
        // Arrange
        var parameters = JsonSerializer.Serialize(new
        {
            latitude = 14.5,
            longitude = 121.0,
            radiusMeters = 5000.0
        });

        var expectedResponse = new ApiResponse<AreaBroadbandInfoDto>
        {
            Succeeded = true,
            Data = new AreaBroadbandInfoDto()
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPointBroadbandInfoRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.ExecuteToolAsync("get_point_broadband_info", parameters);

        // Assert
        result.Should().NotBeNullOrEmpty();
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetPointBroadbandInfoRequest>(r =>
                r.PointRadius.Latitude == 14.5 &&
                r.PointRadius.Longitude == 121.0 &&
                r.PointRadius.RadiusMeters == 5000.0),
            default), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_GenerateConnectivityAssessment_CallsMediatorWithCorrectRequest()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var parameters = JsonSerializer.Serialize(new
        {
            locationIds = new[] { locationId.ToString() },
            locationType = "Barangay",
            forceRecalculate = true
        });

        var expectedResponse = new ApiResponse<List<LocationConnectivityDto>>
        {
            Succeeded = true,
            Data = new List<LocationConnectivityDto>()
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GenerateConnectivityAssessmentRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.ExecuteToolAsync("generate_connectivity_assessment", parameters);

        // Assert
        result.Should().NotBeNullOrEmpty();
        _mediatorMock.Verify(m => m.Send(
            It.Is<GenerateConnectivityAssessmentRequest>(r =>
                r.LocationIds.Contains(locationId) &&
                r.LocationType == LocationTypeEnum.Barangay &&
                r.ForceRecalculate == true),
            default), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_UnknownTool_ReturnsErrorJson()
    {
        // Arrange
        var parameters = "{}";

        // Act
        var result = await _service.ExecuteToolAsync("unknown_tool", parameters);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(result);
        errorResponse.TryGetProperty("error", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteToolAsync_InvalidParameters_ReturnsErrorJson()
    {
        // Arrange
        var invalidParameters = "not valid json";

        // Act
        var result = await _service.ExecuteToolAsync("get_providers", invalidParameters);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(result);
        errorResponse.TryGetProperty("error", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteToolAsync_GetProviders_WithOptionalParameters_ParsesCorrectly()
    {
        // Arrange
        var serviceId1 = Guid.NewGuid();
        var serviceId2 = Guid.NewGuid();
        var parameters = JsonSerializer.Serialize(new
        {
            pageSize = 10,
            pageNumber = 1,
            serviceIds = new[] { serviceId1.ToString(), serviceId2.ToString() },
            names = new[] { "Provider1", "Provider2" }
        });

        var expectedResponse = new PaginatedResponse<ProviderDto>(
            new List<ProviderDto>(),
            pageNumber: 1,
            pageSize: 10,
            totalRows: 0
        );

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllProvidersRequest>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.ExecuteToolAsync("get_providers", parameters);

        // Assert
        result.Should().NotBeNullOrEmpty();
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetAllProvidersRequest>(r =>
                r.ServiceIds != null && r.ServiceIds.Count == 2 &&
                r.Names != null && r.Names.Count == 2),
            default), Times.Once);
    }
}
