using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

/// <summary>
/// Query to get wireless broadband data report (municipalities with barangays, not file download).
/// Returns paginated data similar to wired broadband report.
/// </summary>
public class GetWirelessBroadbandDataReportQuery : IRequest<ApiResponse<WirelessBroadbandDataReportDto>>
{
    public Guid? MunicipalityId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? RegionId { get; set; }

    /// <summary>
    /// When true, includes connectivity scores and service status in the report.
    /// Default is true for scored reports.
    /// </summary>
    public bool IncludeScores { get; set; } = true;

    /// <summary>
    /// Filter by service status (optional). Only applicable when IncludeScores is true.
    /// </summary>
    public ServiceStatusEnum? FilterByStatus { get; set; }

    /// <summary>
    /// Page number for pagination (default: 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size for pagination (default: 10, max: 100)
    /// </summary>
    public int PageSize { get; set; } = 10;
}
