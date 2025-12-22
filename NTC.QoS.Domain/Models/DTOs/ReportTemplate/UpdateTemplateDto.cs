using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.Domain.Models.DTOs.ReportTemplate;

public class UpdateTemplateDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? Instruction { get; set; }

    public bool? IsActive { get; set; }
}
