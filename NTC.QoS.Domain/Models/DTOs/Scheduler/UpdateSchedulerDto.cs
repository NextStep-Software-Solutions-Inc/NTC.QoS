using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Scheduler;

/// <summary>
/// DTO for updating an existing schedule
/// </summary>
public class UpdateSchedulerDto
{
    /// <summary>
    /// User-friendly name for the schedule
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional description of the schedule
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Frequency of the schedule
    /// </summary>
    public ScheduleFrequencyEnum? Frequency { get; set; }

    /// <summary>
    /// Location type for the assessment
    /// </summary>
    public LocationTypeEnum? LocationType { get; set; }

    /// <summary>
    /// List of location IDs to process
    /// </summary>
    public List<Guid>? LocationIds { get; set; }

    /// <summary>
    /// Time zone for the schedule
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// Status of the scheduler
    /// </summary>
    public SchedulerStatusEnum? Status { get; set; }

    /// <summary>
    /// Whether to send notifications when the job completes
    /// </summary>
    public bool? SendNotifications { get; set; }

    /// <summary>
    /// Email addresses to notify
    /// </summary>
    public List<string>? NotificationEmails { get; set; }

    /// <summary>
    /// Schedule start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Schedule end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Schedule configuration based on frequency
    /// </summary>
    public ScheduleConfigurationDto? Configuration { get; set; }

    /// <summary>
    /// Whether to generate hierarchical assessments (include child locations)
    /// </summary>
    public bool? GenerateHierarchicalAssessments { get; set; }

    /// <summary>
    /// Depth of hierarchy to include (1-3)
    /// </summary>
    public int? HierarchicalDepth { get; set; }

    /// <summary>
    /// Maximum number of hierarchical locations to process
    /// </summary>
    public int? MaxHierarchicalLocations { get; set; }

    /// <summary>
    /// Whether to force recalculation of existing assessments
    /// </summary>
    public bool? ForceRecalculate { get; set; }
}
