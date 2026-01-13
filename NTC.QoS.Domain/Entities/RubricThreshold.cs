using NTC.QoS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Represents a scoring threshold rule for a rubric criterion
/// </summary>
public class RubricThreshold : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent rubric criterion
    /// </summary>
    [Required]
    [ForeignKey(nameof(RubricCriterion))]
    public Guid RubricCriterionId { get; set; }

    /// <summary>
    /// Score value assigned when this threshold is met (e.g., 3=Served, 2=Underserved, 1=Unserved, 0=NoData)
    /// </summary>
    public int ScoreValue { get; set; }

    /// <summary>
    /// Comparison operator for threshold evaluation
    /// </summary>
    [Required]
    public ThresholdOperatorEnum Operator { get; set; }

    /// <summary>
    /// Primary threshold value for comparison
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal PrimaryValue { get; set; }

    /// <summary>
    /// Secondary threshold value for range operators (e.g., Between, NotBetween)
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? SecondaryValue { get; set; }

    /// <summary>
    /// Type of metric being evaluated (e.g., "MedianDownloadSpeedMbps", "LatencyMs", "MajorTelcoCount")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MetricType { get; set; } = string.Empty;

    /// <summary>
    /// Optional JSON structure for complex multi-condition logic
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ConditionLogic { get; set; }

    /// <summary>
    /// Human-readable description of this threshold rule
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Evaluation order (lower values evaluated first; typically evaluate highest scores first)
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Navigation property to the parent rubric criterion
    /// </summary>
    public virtual RubricCriterion RubricCriterion { get; set; } = null!;
}
