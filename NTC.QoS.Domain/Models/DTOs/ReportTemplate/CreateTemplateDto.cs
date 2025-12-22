using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.Domain.Models.DTOs.ReportTemplate;

public class CreateTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Instruction { get; set; } = string.Empty;
}
