using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Scheduler;

/// <summary>
/// Response DTO for scheduler information
/// </summary>
public class SchedulerResponseDto
{
    public Guid Id { get; set; }
    public string HangfireJobId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ScheduleFrequencyEnum Frequency { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public SchedulerStatusEnum Status { get; set; }
    public LocationTypeEnum LocationType { get; set; }
    public List<Guid> LocationIds { get; set; } = new();
    public DateTime? LastExecutionTime { get; set; }
    public DateTime? NextExecutionTime { get; set; }
    public int ExecutionCount { get; set; }
    public int FailureCount { get; set; }
    public string? LastError { get; set; }
    public bool SendNotifications { get; set; }
    public List<string>? NotificationEmails { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ScheduleConfigurationDto? Configuration { get; set; }
    public bool GenerateHierarchicalAssessments { get; set; }
    public int HierarchicalDepth { get; set; }
    public int? MaxHierarchicalLocations { get; set; }
    public bool ForceRecalculate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
