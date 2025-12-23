using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// DTO for wireless broadband report job status
/// </summary>
public class WirelessBroadbandReportJobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public ReportJobStatusEnum Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string StatusDescription => GetStatusDescription();
    public DateTime LastUpdated { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? CurrentStep { get; set; }
    public ReportFormatEnum Format { get; set; }
    public string? FilePath { get; set; }
    public string? DownloadUrl { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? BarangaysProcessed { get; set; }
    public int? TotalBarangays { get; set; }
    public int ProgressPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FileName { get; set; }

    private string GetStatusDescription()
    {
        return Status switch
        {
            ReportJobStatusEnum.Queued => "Report generation job is queued and waiting to start.",
            ReportJobStatusEnum.Processing => $"Generating report... {(CurrentStep ?? "Initializing")}",
            ReportJobStatusEnum.Completed => "Report generation completed successfully. Ready for download.",
            ReportJobStatusEnum.Failed => $"Report generation failed. {(ErrorMessage ?? "Unknown error")}",
            _ => "Unknown status"
        };
    }
}
