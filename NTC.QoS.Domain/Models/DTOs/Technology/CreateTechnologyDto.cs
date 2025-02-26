namespace NTC.QoS.Domain.Models.DTOs.Technology;

public class CreateTechnologyDto
{
    public Guid GenerationId { get; set; }

    public string Standard { get; set; } = string.Empty;
    public string? Description { get; set; }
}
