using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Status information for connectivity gap report generation background jobs
/// </summary>
public class ConnectivityGapReportJobStatusDto
{
    /// <summary>
    /// Unique identifier for the job (Hangfire Job ID)
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the job
    /// </summary>
    public ConnectivityGapReportJobStatusEnum Status { get; set; }

    /// <summary>
    /// Progress message or error details
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the status was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Timestamp when the job was started
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Timestamp when the job was completed (success or failure)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total processing time (if job is completed)
    /// </summary>
    public TimeSpan? ProcessingTime => EndTime.HasValue && StartTime.HasValue
        ? EndTime.Value - StartTime.Value
        : null;

    /// <summary>
    /// Current step being processed
    /// </summary>
    public string? CurrentStep { get; set; }

    /// <summary>
    /// Percentage of completion (0-100)
    /// </summary>
    public double? ProgressPercentage { get; set; }

    /// <summary>
    /// Number of areas processed
    /// </summary>
    public int? AreasProcessed { get; set; }

    /// <summary>
    /// Total number of areas to process
    /// </summary>
    public int? TotalAreas { get; set; }

    /// <summary>
    /// Report format being generated
    /// </summary>
    public ReportFormat? Format { get; set; }

    /// <summary>
    /// File path in blob storage (when completed)
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Download URL for the generated report (when completed)
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// File size in bytes (when completed)
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Whether the job is currently running
    /// </summary>
    public bool IsRunning => Status == ConnectivityGapReportJobStatusEnum.Processing;

    /// <summary>
    /// Whether the job is completed (success or failure)
    /// </summary>
    public bool IsCompleted => Status == ConnectivityGapReportJobStatusEnum.Completed ||
                               Status == ConnectivityGapReportJobStatusEnum.Failed;

    /// <summary>
    /// User-friendly status description
    /// </summary>
    public string StatusDescription => Status switch
    {
        ConnectivityGapReportJobStatusEnum.Pending => "Job is queued and waiting to start",
        ConnectivityGapReportJobStatusEnum.Processing => "Job is currently generating the report",
        ConnectivityGapReportJobStatusEnum.Completed => "Report generated successfully",
        ConnectivityGapReportJobStatusEnum.Failed => "Job failed with errors",
        ConnectivityGapReportJobStatusEnum.Cancelled => "Job was cancelled",
        _ => "Unknown status"
    };

    /// <summary>
    /// Formatted file size string (when completed)
    /// </summary>
    public string? FileSizeFormatted
    {
        get
        {
            if (!FileSizeBytes.HasValue) return null;

            var size = FileSizeBytes.Value;
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = size;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
