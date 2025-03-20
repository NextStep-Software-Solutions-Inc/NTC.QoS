namespace NTC.QoS.Domain.Models.DTOs.Study;

public class AreaStudyStudyDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string? Description { get; set; }
}