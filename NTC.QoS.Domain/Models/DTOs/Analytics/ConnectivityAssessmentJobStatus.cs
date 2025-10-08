using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Status information for connectivity assessment background jobs
/// </summary>
public class ConnectivityAssessmentJobStatus
{
    /// <summary>
    /// Unique identifier for the job
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the job
    /// </summary>
    public ConnectivityAssessmentJobStatusEnum Status { get; set; }

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
    /// Number of locations processed (if available)
    /// </summary>
    public int? ProcessedCount { get; set; }

    /// <summary>
    /// Total number of locations to process (if available)
    /// </summary>
    public int? TotalCount { get; set; }

    /// <summary>
    /// Progress percentage (if counts are available)
    /// </summary>
    public double? ProgressPercentage => ProcessedCount.HasValue && TotalCount.HasValue && TotalCount.Value > 0
        ? Math.Round((double)ProcessedCount.Value / TotalCount.Value * 100, 1)
        : null;

    /// <summary>
    /// Indicates if the job is still running
    /// </summary>
    public bool IsRunning => Status == ConnectivityAssessmentJobStatusEnum.Processing;

    /// <summary>
    /// Indicates if the job has completed (successfully or with failure)
    /// </summary>
    public bool IsCompleted => Status == ConnectivityAssessmentJobStatusEnum.Completed || 
                               Status == ConnectivityAssessmentJobStatusEnum.Failed;

    /// <summary>
    /// User-friendly status description
    /// </summary>
    public string StatusDescription => Status switch
    {
        ConnectivityAssessmentJobStatusEnum.Pending => "Job is queued and waiting to start",
        ConnectivityAssessmentJobStatusEnum.Processing => "Job is currently processing assessments",
        ConnectivityAssessmentJobStatusEnum.Completed => "Job completed successfully",
        ConnectivityAssessmentJobStatusEnum.Failed => "Job failed with errors",
        _ => "Unknown status"
    };
}