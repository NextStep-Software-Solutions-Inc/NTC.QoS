using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Domain.Models.DTOs.Study;

public class StudyDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; } 
    public string? Description { get; set; }
    public AreaStudyDto? AreaStudy { get; set; }
    public required decimal StudyRadius { get; set; }
}
