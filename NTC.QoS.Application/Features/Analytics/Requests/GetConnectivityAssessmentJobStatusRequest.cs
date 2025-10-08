using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Analytics.Requests;

/// <summary>
/// Request to get the status of a connectivity assessment background job
/// </summary>
public class GetConnectivityAssessmentJobStatusRequest : IRequest<ApiResponse<ConnectivityAssessmentJobStatus>>
{
    public string JobId { get; set; } = string.Empty;
}