using NTC.QoS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Entity for managing scheduled connectivity assessment jobs
/// Allows admins to configure automatic execution of connectivity assessments
/// </summary>
public class ConnectivityAssessmentScheduler : BaseEntity
{
    /// <summary>
    /// Unique identifier for the scheduled job in Hangfire
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string HangfireJobId { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly name for the schedule
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the schedule
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Frequency of the schedule (Hourly, Daily, Weekly, Monthly, Custom)
    /// </summary>
    [Required]
    public ScheduleFrequencyEnum Frequency { get; set; }

    /// <summary>
    /// Cron expression for the schedule (generated based on frequency or custom)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Time zone for the schedule (e.g., "Asia/Manila")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Status of the scheduler (Active, Paused, Completed, Failed)
    /// </summary>
    [Required]
    public SchedulerStatusEnum Status { get; set; } = SchedulerStatusEnum.Active;

    /// <summary>
    /// Location type for the assessment (Region, Province, Municipality, Barangay)
    /// </summary>
    [Required]
    public LocationTypeEnum LocationType { get; set; }

    /// <summary>
    /// JSON array of location IDs to process
    /// Stored as JSON to maintain flexibility
    /// </summary>
    [Required]
    public string LocationIdsJson { get; set; } = "[]";

    /// <summary>
    /// Last time this schedule executed successfully
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }

    /// <summary>
    /// Next scheduled execution time
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }

    /// <summary>
    /// Number of times this schedule has executed
    /// </summary>
    public int ExecutionCount { get; set; } = 0;

    /// <summary>
    /// Number of times this schedule has failed
    /// </summary>
    public int FailureCount { get; set; } = 0;

    /// <summary>
    /// Last error message if the schedule failed
    /// </summary>
    [MaxLength(2000)]
    public string? LastError { get; set; }

    /// <summary>
    /// Whether to send notifications when the job completes
    /// </summary>
    public bool SendNotifications { get; set; } = false;

    /// <summary>
    /// Email addresses to notify (JSON array)
    /// </summary>
    [MaxLength(1000)]
    public string? NotificationEmailsJson { get; set; }

    /// <summary>
    /// Schedule start date (optional)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Schedule end date (optional)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Additional configuration options (JSON)
    /// Can include things like specific days of week, specific time of day, etc.
    /// </summary>
    [MaxLength(2000)]
    public string? ConfigurationJson { get; set; }
}
