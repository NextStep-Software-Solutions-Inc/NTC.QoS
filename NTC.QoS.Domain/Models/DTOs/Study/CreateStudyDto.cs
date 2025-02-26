namespace NTC.QoS.Domain.Models.DTOs.Study;

public class CreateStudyDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AreaStudyId { get; set; }
    public decimal StudyRadius { get; set; }

}
