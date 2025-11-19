using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Request object for background connectivity assessment jobs
/// Contains all necessary information to process connectivity assessments
/// </summary>
public class ConnectivityAssessmentJobRequestDto
{
    /// <summary>
    /// Unique identifier for this job
    /// </summary>
    public int JobId { get; set; }

    /// <summary>
    /// List of location IDs to process
    /// </summary>
    public List<Guid> LocationIds { get; set; } = [];

    /// <summary>
    /// Type of locations being processed
    /// </summary>
    public LocationTypeEnum LocationType { get; set; }

    /// <summary>
    /// Whether to force recalculation of existing assessments
    /// </summary>
    public bool ForceRecalculate { get; set; } = false;

    /// <summary>
    /// Whether to generate assessments for child locations hierarchically
    /// </summary>
    public bool GenerateHierarchicalAssessments { get; set; } = false;

    /// <summary>
    /// Depth of hierarchical processing (1-3 levels)
    /// </summary>
    public int HierarchicalDepth { get; set; } = 3;

    /// <summary>
    /// NEW: Whether to enable hierarchical processing (default disabled for performance)
    /// This is separate from GenerateHierarchicalAssessments for backward compatibility
    /// </summary>
    public bool EnableHierarchicalProcessing { get; set; } = false;

    /// <summary>
    /// NEW: Maximum number of hierarchical locations to process (performance limit)
    /// </summary>
    public int MaxHierarchicalLocations { get; set; } = 1000;

    /// <summary>
    /// NEW: Batch size for hierarchical processing (smaller than primary processing)
    /// </summary>
    public int HierarchicalBatchSize { get; set; } = 50;

    /// <summary>
    /// Start date for filtering speedtest and wireless validation data (inclusive).
    /// If null, no start date filter is applied.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering speedtest and wireless validation data (inclusive).
    /// If null, no end date filter is applied.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// User ID who requested the job
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// When this job was requested
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Maximum number of locations to process in parallel
    /// </summary>
    public int MaxParallelism { get; set; } = 5;
}