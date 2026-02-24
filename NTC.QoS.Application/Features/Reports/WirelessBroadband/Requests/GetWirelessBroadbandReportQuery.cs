using MediatR;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

/// <summary>
/// Query to generate wireless broadband report with optional connectivity scores.
/// When IncludeScores is true, the report includes service status and connectivity scoring.
/// </summary>
public class GetWirelessBroadbandReportQuery : IRequest<ApiResponse<WirelessBroadbandReportResult>>
{
    public Guid? MunicipalityId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? RegionId { get; set; }
    public ReportFormat Format { get; set; } = ReportFormat.Excel;

    /// <summary>
    /// When true, includes connectivity scores and service status in the report.
    /// Default is true for backward compatibility with scored reports.
    /// </summary>
    public bool IncludeScores { get; set; } = true;

    /// <summary>
    /// Filter by service status (optional). Only applicable when IncludeScores is true.
    /// </summary>
    public ServiceStatusEnum? FilterByStatus { get; set; }

    public GetWirelessBroadbandReportQuery() { }
}
