using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Status information for connectivity assessment background jobs
/// </summary>
public class ConnectivityAssessmentJobStatusDto
{
    /// <summary>
    /// Unique identifier for the job (using int for easier querying)
    /// </summary>
    public int JobId { get; set; }

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
    /// Number of locations actually processed (updated during job execution)
    /// For completed jobs, this represents the actual total that was processable
    /// </summary>
    public int? ProcessedCount { get; set; }

    /// <summary>
    /// Initial estimated total number of locations to process (set at job creation)
    /// This may differ from the actual count once job determines what's processable
    /// </summary>
    public int? EstimatedTotalCount { get; set; }

    /// <summary>
    /// Progress percentage based on processed count vs estimated total
    /// For completed jobs, always returns 100% since all available items were processed
    /// For running jobs, calculates based on ProcessedCount vs EstimatedTotalCount
    /// </summary>
    public double? ProgressPercentage
    {
        get
        {
            if (!ProcessedCount.HasValue) return null;
            
            // For completed jobs, progress is always 100%
            if (Status == ConnectivityAssessmentJobStatusEnum.Completed)
            {
                return 100.0;
            }
            
            // For running or failed jobs, calculate based on estimate
            return EstimatedTotalCount.HasValue && EstimatedTotalCount.Value > 0
                ? Math.Round((double)ProcessedCount.Value / EstimatedTotalCount.Value * 100, 1)
                : null;
        }
    }

    /// <summary>
    /// Indicates whether we're still working with estimated counts (true) or have actual results (false)
    /// For completed/failed jobs, ProcessedCount represents the definitive actual count
    /// </summary>
    public bool IsCountEstimated => Status != ConnectivityAssessmentJobStatusEnum.Completed && 
                                    Status != ConnectivityAssessmentJobStatusEnum.Failed;

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
        ConnectivityAssessmentJobStatusEnum.Cancelled => "Job was cancelled",
        _ => "Unknown status"
    };
}