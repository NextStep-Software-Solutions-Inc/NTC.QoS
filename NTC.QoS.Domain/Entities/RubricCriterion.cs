using NTC.QoS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Represents an individual scoring criterion within a rubric configuration
/// </summary>
public class RubricCriterion : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent rubric configuration
    /// </summary>
    [Required]
    [ForeignKey(nameof(RubricConfiguration))]
    public Guid RubricConfigurationId { get; set; }

    /// <summary>
    /// Type of criterion being evaluated
    /// </summary>
    [Required]
    public CriterionTypeEnum CriterionType { get; set; }

    /// <summary>
    /// Display name of the criterion
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what this criterion evaluates
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Maximum achievable score for this criterion
    /// </summary>
    public int MaxScore { get; set; } = 3;

    /// <summary>
    /// Weight multiplier for weighted scoring models (default 1.0 for equal weighting)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>
    /// Display order for this criterion in the rubric
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Indicates if this criterion is currently enabled for scoring
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Navigation property to the parent rubric configuration
    /// </summary>
    public virtual RubricConfiguration RubricConfiguration { get; set; } = null!;

    /// <summary>
    /// Collection of scoring thresholds for this criterion
    /// </summary>
    public virtual ICollection<RubricThreshold> Thresholds { get; set; } = [];
}
