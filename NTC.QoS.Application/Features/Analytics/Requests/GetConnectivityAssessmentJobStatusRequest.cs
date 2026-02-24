using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Requests;

/// <summary>
/// Request to get the status of a connectivity assessment background job
/// </summary>
public class GetConnectivityAssessmentJobStatusRequest : IRequest<ApiResponse<ConnectivityAssessmentJobStatusDto>>
{
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Force database verification even if already verified
    /// Useful for checking current database state after issues
    /// </summary>
    public bool ForceVerification { get; set; } = false;
}