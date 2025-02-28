namespace NTC.QoS.Domain.Models.DTOs.Study;

public class StudyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AreaStudyId { get; set; }
    public virtual AreaStudyDto AreaStudy { get; set; } = new();
    public decimal StudyRadius { get; set; }
}