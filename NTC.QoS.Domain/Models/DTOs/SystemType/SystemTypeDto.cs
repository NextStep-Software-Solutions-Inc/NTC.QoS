namespace NTC.QoS.Domain.Models.DTOs.SystemType;

public class SystemTypeDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public STTechnologyDto? Technology { get; set; }
}

public class STTechnologyDto
{
    public required Guid Id { get; set; }
    public required string Standard { get; set; } 
    public required string Description { get; set; }
    public STGenerationDto? Generation { get; set; }
}

public class STGenerationDto
{ 
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}
