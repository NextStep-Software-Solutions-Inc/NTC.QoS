using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Requests;

/// <summary>
/// Request to get the status of a connectivity assessment background job
/// </summary>
public class GetConnectivityAssessmentJobStatusRequest : IRequest<ApiResponse<ConnectivityAssessmentJobStatusDto>>
{
    public int JobId { get; set; }
}