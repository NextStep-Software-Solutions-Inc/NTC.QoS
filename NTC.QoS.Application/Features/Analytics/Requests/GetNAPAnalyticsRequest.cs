using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Requests;

/// <summary>
/// Request for NAP Analytics Dashboard
/// </summary>
public class GetNAPAnalyticsRequest : IRequest<ApiResponse<NAPAnalyticsResponseDto>>
{
    /// <summary>
    /// Optional: Filter by specific barangay IDs
    /// </summary>
    public List<Guid>? BarangayIds { get; set; }

    /// <summary>
    /// Optional: Filter by specific municipality IDs
    /// </summary>
    public List<Guid>? MunicipalityIds { get; set; }

    /// <summary>
    /// Optional: Filter by specific province IDs
    /// </summary>
    public List<Guid>? ProvinceIds { get; set; }

    /// <summary>
    /// Optional: Filter by specific region IDs
    /// </summary>
    public List<Guid>? RegionIds { get; set; }

    /// <summary>
    /// Optional: Filter by specific service provider IDs
    /// </summary>
    public List<Guid>? ServiceProviderIds { get; set; }

    /// <summary>
    /// Include geographic coverage calculation (can be resource-intensive)
    /// </summary>
    public bool IncludeGeographicCoverage { get; set; } = true;
}
