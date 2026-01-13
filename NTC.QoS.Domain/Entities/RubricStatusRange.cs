using NTC.QoS.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Represents a status range definition that maps total scores to connectivity statuses
/// </summary>
public class RubricStatusRange : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent rubric configuration
    /// </summary>
    [Required]
    [ForeignKey(nameof(RubricConfiguration))]
    public Guid RubricConfigurationId { get; set; }

    /// <summary>
    /// Name of the connectivity status (e.g., "Served", "Underserved", "Unserved")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string StatusName { get; set; } = string.Empty;

    /// <summary>
    /// Minimum score (inclusive) for this status range
    /// </summary>
    public int MinScore { get; set; }

    /// <summary>
    /// Maximum score (inclusive) for this status range
    /// </summary>
    public int MaxScore { get; set; }

    /// <summary>
    /// Integer value mapping to ConnectivityStatusEnum (3=Served, 2=Underserved, 1=Unserved)
    /// </summary>
    public int StatusValue { get; set; }

    /// <summary>
    /// Hex color code for UI display (e.g., "#28A745" for green)
    /// </summary>
    [MaxLength(20)]
    public string? DisplayColor { get; set; }

    /// <summary>
    /// Human-readable description of this status level
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order for this status range
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Navigation property to the parent rubric configuration
    /// </summary>
    public virtual RubricConfiguration RubricConfiguration { get; set; } = null!;
}
