namespace NTC.QoS.Domain.Models.DTOs.Study;

public class CreateStudyDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required Guid AreaStudyId { get; set; }
    public required decimal StudyRadius { get; set; }
}
