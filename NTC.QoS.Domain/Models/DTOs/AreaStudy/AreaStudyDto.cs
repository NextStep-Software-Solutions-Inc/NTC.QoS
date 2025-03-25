namespace NTC.QoS.Domain.Models.DTOs.AreaStudy;

public class AreaStudyDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string? Description { get; set; }
}
