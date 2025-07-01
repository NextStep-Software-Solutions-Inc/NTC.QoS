namespace NTC.QoS.Domain.Models.DTOs.Generation;

public class GenerationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
