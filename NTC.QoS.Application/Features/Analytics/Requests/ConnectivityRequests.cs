using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Requests;

public class GetConnectivityAnalysisRequest : IRequest<ApiResponse<ConnectivityAnalysisResponseDto>>
{
    public List<Guid> LocationIds { get; set; } = [];
    public LocationTypeEnum LocationType { get; set; }
    public ConnectivityStatusEnum? FilterByStatus { get; set; }
    public bool IncludeSpeedTestData { get; set; } = true;
    public bool IncludeInfrastructureData { get; set; } = true;
}

public class GenerateConnectivityAssessmentRequest : IRequest<ApiResponse<List<LocationConnectivityDto>>>
{
    public List<Guid> LocationIds { get; set; } = [];
    public LocationTypeEnum LocationType { get; set; }
    public bool ForceRecalculate { get; set; } = false;
}