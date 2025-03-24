namespace NTC.QoS.Domain.Models.DTOs.Study;

public class StudyDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; } 
    public string? Description { get; set; }
    public required AreaStudyStudyDto AreaStudy { get; set; }
    public decimal StudyRadius { get; set; }
}
