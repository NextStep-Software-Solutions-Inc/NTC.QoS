namespace NTC.QoS.Domain.Models.DTOs.ReportTemplate;

/// <summary>
/// Summary DTO for listing report templates (without the instruction).
/// </summary>
public class ReportTemplateListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
