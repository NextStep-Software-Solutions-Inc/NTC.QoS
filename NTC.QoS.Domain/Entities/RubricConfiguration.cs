using NTC.QoS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Represents a versioned rubric configuration for connectivity assessment scoring
/// </summary>
public class RubricConfiguration : BaseEntity
{
    /// <summary>
    /// Name of the rubric configuration
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the rubric's purpose and scope
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Semantic version identifier (e.g., "1.0.0", "2.1.3")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this rubric is currently active and can be used for new assessments
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Indicates if this is the default rubric for assessments without explicit rubric assignment
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Date and time when this rubric becomes effective
    /// </summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    /// Date and time when this rubric is no longer effective (null for open-ended)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Optional location type scope for this rubric (null means applicable to all location types)
    /// </summary>
    public LocationTypeEnum? ApplicableLocationType { get; set; }

    /// <summary>
    /// Complete rubric configuration snapshot in JSON format for reproducibility
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ConfigurationJson { get; set; }

    /// <summary>
    /// Collection of scoring criteria for this rubric
    /// </summary>
    public virtual ICollection<RubricCriterion> Criteria { get; set; } = [];

    /// <summary>
    /// Collection of status ranges that map total scores to connectivity statuses
    /// </summary>
    public virtual ICollection<RubricStatusRange> StatusRanges { get; set; } = [];

    /// <summary>
    /// Collection of connectivity assessments that use this rubric configuration
    /// </summary>
    public virtual ICollection<ConnectivityAssessment> ConnectivityAssessments { get; set; } = [];
}
