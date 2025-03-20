namespace NTC.QoS.Domain.Models.DTOs.Study;

public class UpdateStudyDto
{
    public Guid? AreaStudyId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? StudyRadius { get; set; }
}
