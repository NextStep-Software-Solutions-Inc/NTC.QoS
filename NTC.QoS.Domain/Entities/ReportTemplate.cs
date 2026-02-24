using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.Domain.Entities;

public class ReportTemplate : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The system prompt or instruction used for AI generation.
    /// </summary>
    [Required]
    public string Instruction { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
