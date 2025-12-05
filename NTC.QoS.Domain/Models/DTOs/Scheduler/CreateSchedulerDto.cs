using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Scheduler;

/// <summary>
/// DTO for creating a new connectivity assessment schedule
/// </summary>
public class CreateSchedulerDto
{
    /// <summary>
    /// User-friendly name for the schedule
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the schedule
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Frequency of the schedule
    /// </summary>
    public ScheduleFrequencyEnum Frequency { get; set; }

    /// <summary>
    /// Location type for the assessment
    /// </summary>
    public LocationTypeEnum LocationType { get; set; }

    /// <summary>
    /// List of location IDs to process
    /// </summary>
    public List<Guid> LocationIds { get; set; } = new();

    /// <summary>
    /// Time zone for the schedule (e.g., "Asia/Manila")
    /// </summary>
    public string TimeZone { get; set; } = "Asia/Manila";

    /// <summary>
    /// Whether to send notifications when the job completes
    /// </summary>
    public bool SendNotifications { get; set; } = false;

    /// <summary>
    /// Email addresses to notify
    /// </summary>
    public List<string>? NotificationEmails { get; set; }

    /// <summary>
    /// Schedule start date (optional)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Schedule end date (optional)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Schedule configuration based on frequency
    /// </summary>
    public ScheduleConfigurationDto Configuration { get; set; } = new();

    /// <summary>
    /// Whether to generate hierarchical assessments (include child locations)
    /// </summary>
    public bool GenerateHierarchicalAssessments { get; set; } = true;

    /// <summary>
    /// Depth of hierarchy to include (1-3)
    /// </summary>
    public int HierarchicalDepth { get; set; } = 3;

    /// <summary>
    /// Maximum number of hierarchical locations to process.
    /// Leave null (default) to process ALL child locations for complete coverage.
    /// Set a value only if you need to limit processing for performance reasons.
    /// Warning: Setting a limit may result in incomplete connectivity assessments.
    /// </summary>
    public int? MaxHierarchicalLocations { get; set; } = null;

    /// <summary>
    /// Whether to force recalculation of existing assessments
    /// </summary>
    public bool ForceRecalculate { get; set; } = false;
}

/// <summary>
/// Configuration options for schedule frequency
/// </summary>
public class ScheduleConfigurationDto
{
    // For Hourly
    /// <summary>
    /// Minute of the hour (0-59) - for hourly schedules
    /// </summary>
    public int? MinuteOfHour { get; set; }

    // For Daily
    /// <summary>
    /// Hour of the day (0-23) - for daily schedules
    /// </summary>
    public int? HourOfDay { get; set; }

    /// <summary>
    /// Minute of the hour (0-59) - for daily schedules
    /// </summary>
    public int? MinuteOfDay { get; set; }

    // For Weekly
    /// <summary>
    /// Days of week to run (0=Sunday, 1=Monday, etc.) - for weekly schedules
    /// </summary>
    public List<int>? DaysOfWeek { get; set; }

    /// <summary>
    /// Hour of the day (0-23) - for weekly schedules
    /// </summary>
    public int? WeeklyHour { get; set; }

    /// <summary>
    /// Minute of the hour (0-59) - for weekly schedules
    /// </summary>
    public int? WeeklyMinute { get; set; }

    // For Monthly
    /// <summary>
    /// Day of the month (1-31) - for monthly schedules
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Hour of the day (0-23) - for monthly schedules
    /// </summary>
    public int? MonthlyHour { get; set; }

    /// <summary>
    /// Minute of the hour (0-59) - for monthly schedules
    /// </summary>
    public int? MonthlyMinute { get; set; }

    // For Custom
    /// <summary>
    /// Custom cron expression - for custom schedules
    /// </summary>
    public string? CustomCronExpression { get; set; }
}
