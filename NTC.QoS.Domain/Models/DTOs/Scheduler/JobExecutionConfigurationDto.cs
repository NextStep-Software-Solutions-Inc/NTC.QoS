namespace NTC.QoS.Domain.Models.DTOs.Scheduler;

/// <summary>
/// Configuration for job execution behavior (hierarchical assessments, recalculation, etc.)
/// This is separate from schedule timing configuration
/// </summary>
public class JobExecutionConfigurationDto
{
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
    /// NULL = No limit (process all child locations - recommended for complete coverage)
    /// VALUE = Apply limit (use only if you need to cap processing for performance reasons)
    /// </summary>
    public int? MaxHierarchicalLocations { get; set; } = null;

    /// <summary>
    /// Whether to force recalculation of existing assessments
    /// </summary>
    public bool ForceRecalculate { get; set; } = false;

    /// <summary>
    /// Schedule timing configuration
    /// </summary>
    public ScheduleConfigurationDto ScheduleConfiguration { get; set; } = new();
}
