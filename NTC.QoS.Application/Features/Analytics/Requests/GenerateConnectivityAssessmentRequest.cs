using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Requests;

public class GenerateConnectivityAssessmentRequest : IRequest<ApiResponse<List<LocationConnectivityDto>>>
{
    public List<Guid> LocationIds { get; set; } = [];
    public LocationTypeEnum LocationType { get; set; }
    public bool ForceRecalculate { get; set; } = false;

    /// <summary>
    /// Start date for filtering speedtest and wireless validation data (inclusive).
    /// If null, no start date filter is applied.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering speedtest and wireless validation data (inclusive).
    /// If null, no end date filter is applied.
    /// </summary>
    public DateTime? EndDate { get; set; }
}