using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.Connectivity.Requests;

/// <summary>
/// Query to retrieve the unified connectivity report combining asset and gap data as JSON data
/// </summary>
public class GetConnectivityReportQuery : IRequest<ApiResponse<ConnectivityReportDto>>
{
    /// <summary>
    /// Optional filter by Region ID
    /// </summary>
    public Guid? RegionId { get; set; }

    /// <summary>
    /// Optional filter by Province ID
    /// </summary>
    public Guid? ProvinceId { get; set; }

    /// <summary>
    /// Optional filter by Municipality ID
    /// </summary>
    public Guid? MunicipalityId { get; set; }

    /// <summary>
    /// Optional filter by connectivity status. When null, all statuses (Served, Underserved, Unserved) are included.
    /// Served = 3, Underserved = 2, Unserved = 1
    /// </summary>
    public ConnectivityStatusEnum? FilterByStatus { get; set; }

    /// <summary>
    /// Page number for pagination (default: 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size for pagination (default: 10, max: 100)
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// When <c>true</c> (default), the response includes <c>MunicipalitySummaries</c>,
    /// <c>ProvinceSummaries</c>, and <c>RegionSummary</c> computed from the full unfiltered dataset.
    /// Set to <c>false</c> on subsequent paginated requests where geographic summaries are not needed
    /// to avoid the extra database query cost of loading all assessment location details.
    /// </summary>
    public bool IncludeSummaries { get; set; } = true;
}
