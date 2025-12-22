using MediatR;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

/// <summary>
/// Query to generate wireless broadband report with connectivity scores
/// </summary>
public class GetWirelessBroadbandScoredReportQuery : IRequest<ApiResponse<WirelessBroadbandReportResult>>
{
    public Guid? RegionId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? MunicipalityId { get; set; }

    /// <summary>
    /// Report format (Excel or PDF)
    /// </summary>
    public ReportFormat Format { get; set; } = ReportFormat.Excel;

    /// <summary>
    /// Filter by service status (optional)
    /// </summary>
    public ServiceStatusEnum? FilterByStatus { get; set; }
}