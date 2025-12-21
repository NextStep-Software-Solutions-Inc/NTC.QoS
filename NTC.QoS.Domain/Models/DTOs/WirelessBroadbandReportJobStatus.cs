using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Domain.Models.DTOs;

/// <summary>
/// In-memory status tracking for wireless broadband report jobs
/// </summary>
public class WirelessBroadbandReportJobStatus
{
    public string JobId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ReportJobStatusEnum Status { get; set; }
    public Guid? RegionId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? MunicipalityId { get; set; }
    public ReportFormatEnum Format { get; set; }
    public ServiceStatusEnum? FilterByStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
}
