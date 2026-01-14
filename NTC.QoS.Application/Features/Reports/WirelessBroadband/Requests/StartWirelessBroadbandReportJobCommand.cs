using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

/// <summary>
/// Command to start a wireless broadband report generation job
/// </summary>
public class StartWirelessBroadbandReportJobCommand : IRequest<string>
{
    public Guid? RegionId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? MunicipalityId { get; set; }
    public ServiceStatusEnum? FilterByStatus { get; set; }
    public ReportFormatEnum Format { get; set; }
    public bool IncludeScores { get; set; }
}
