using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Status information for connectivity assessment background jobs
/// </summary>
public class ConnectivityAssessmentJobStatusDto
{
    /// <summary>
    /// Unique identifier for the job (Hangfire Job ID)
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
    /// Number of locations currently being processed (optimistic count, includes items not yet saved to database)
    /// This count increments immediately as items are processed, before batch save
    /// Used for real-time progress feedback to users
    /// </summary>
    public int? ProcessingCount { get; set; }

    /// <summary>
    /// Number of locations actually processed and saved to database (pessimistic/accurate count)
    /// This count increments only after successful batch saves to database
    /// For completed jobs, this represents the actual total that was successfully saved
    /// </summary>
    public int? ProcessedCount { get; set; }

    /// <summary>
    /// Total number of locations to process (calculated from database before job starts)
    /// This is the ACTUAL count determined by querying child location counts upfront
    /// Not an estimate - represents the real total that will be processed
    /// </summary>
    public int? TotalCount { get; set; }

    /// <summary>
    /// Progress percentage based on saved (processed) count vs total count
    /// For completed jobs, always returns 100% since all available items were processed
    /// For running jobs, calculates based on ProcessedCount (saved) vs TotalCount
    /// This is the pessimistic/accurate progress percentage
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

            // For running or failed jobs, calculate based on actual total
            return TotalCount.HasValue && TotalCount.Value > 0
                ? Math.Round((double)ProcessedCount.Value / TotalCount.Value * 100, 1)
                : null;
        }
    }

    /// <summary>
    /// Processing percentage based on items being processed (optimistic progress)
    /// Shows real-time progress including items not yet saved
    /// This gives more responsive feedback to users during batch processing
    /// </summary>
    public double? ProcessingPercentage
    {
        get
        {
            if (!ProcessingCount.HasValue) return null;

            // For completed jobs, processing percentage matches progress percentage
            if (Status == ConnectivityAssessmentJobStatusEnum.Completed)
            {
                return 100.0;
            }

            // For running jobs, calculate based on processing count
            return TotalCount.HasValue && TotalCount.Value > 0
                ? Math.Round((double)ProcessingCount.Value / TotalCount.Value * 100, 1)
                : null;
        }
    }

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

    /// <summary>
    /// Number of assessments actually found in database (for completed jobs)
    /// Used to detect cache vs database discrepancies
    /// </summary>
    public int? DatabaseVerifiedCount { get; set; }

    /// <summary>
    /// Indicates if database verification was performed
    /// </summary>
    public bool DatabaseVerified { get; set; }

    /// <summary>
    /// Database verification timestamp
    /// </summary>
    public DateTime? DatabaseVerificationTime { get; set; }

    /// <summary>
    /// Indicates if there's a discrepancy between processed count and database count
    /// </summary>
    public bool HasDatabaseDiscrepancy => DatabaseVerified && 
        DatabaseVerifiedCount.HasValue && 
        ProcessedCount.HasValue && 
        DatabaseVerifiedCount.Value < ProcessedCount.Value;

    /// <summary>
    /// Number of assessments missing from database (if discrepancy exists)
    /// </summary>
    public int? MissingInDatabase => HasDatabaseDiscrepancy 
        ? ProcessedCount!.Value - DatabaseVerifiedCount!.Value 
        : null;

    /// <summary>
    /// Indicates if the status is stale (not updated in the last 5 minutes for running jobs)
    /// </summary>
    public bool IsStale
    {
        get
        {
            if (IsCompleted) return false; // Completed jobs don't become stale
            var staleDuration = TimeSpan.FromMinutes(5);
            return DateTime.UtcNow - LastUpdated > staleDuration;
        }
    }

    /// <summary>
    /// Sample of successfully saved location IDs (for verification purposes)
    /// Only populated for completed jobs with database verification
    /// </summary>
    public List<Guid>? SampleLocationIds { get; set; }

    /// <summary>
    /// Number of locations that failed during processing
    /// </summary>
    public int? FailedLocationCount { get; set; }

    /// <summary>
    /// Detailed error messages for failed locations (limited to first 10)
    /// </summary>
    public List<string>? FailedLocationErrors { get; set; }

    /// <summary>
    /// Health status indicator combining all checks
    /// </summary>
    public string HealthStatus
    {
        get
        {
            if (Status == ConnectivityAssessmentJobStatusEnum.Failed) return "Failed";
            if (IsStale) return "Stale";
            if (HasDatabaseDiscrepancy) return "Warning - Data Loss Detected";
            if (Status == ConnectivityAssessmentJobStatusEnum.Completed && DatabaseVerified) return "Healthy";
            if (Status == ConnectivityAssessmentJobStatusEnum.Completed && !DatabaseVerified) return "Unknown - Not Verified";
            if (Status == ConnectivityAssessmentJobStatusEnum.Processing) return "Running";
            return "Pending";
        }
    }
}