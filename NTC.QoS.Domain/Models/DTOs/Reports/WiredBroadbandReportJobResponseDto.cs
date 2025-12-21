using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Response DTO for wired broadband report job creation
/// </summary>
public class WiredBroadbandReportJobResponseDto
{
    /// <summary>
    /// Unique identifier for the job (Hangfire Job ID)
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the job
    /// </summary>
    public WiredBroadbandReportJobStatusEnum Status { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Estimated processing duration in minutes
    /// </summary>
    public int EstimatedDurationMinutes { get; set; }

    /// <summary>
    /// URL to check job status
    /// </summary>
    public string StatusUrl => $"/api/reports/wired-broadband/job/{JobId}/status";

    /// <summary>
    /// URL to download report when completed
    /// </summary>
    public string? DownloadUrl { get; set; }
}
