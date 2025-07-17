using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Domain.Models.DTOs.SystemType;

public class SystemTypeDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public TechnologyDto? Technology { get; set; }
}
