namespace NTC.QoS.Domain.Models.DTOs.SystemType;

public class CreateSystemTypeDto
{
    public required Guid TechnologyId { get; set; }
    public required string Name { get; set; }
}
