namespace NTC.QoS.Domain.Models.DTOs.ReportTemplate;

/// <summary>
/// Full details DTO for a report template including the instruction.
/// </summary>
public class ReportTemplateDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
