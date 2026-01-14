using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Response DTO when starting a wireless broadband report job
/// </summary>
public class WirelessBroadbandReportJobResponseDto
{
    public string JobId { get; set; } = string.Empty;
    public ReportJobStatusEnum Status { get; set; } = ReportJobStatusEnum.Queued;
    public string Message { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
}
