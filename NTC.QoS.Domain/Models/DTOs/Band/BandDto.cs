namespace NTC.QoS.Domain.Models.DTOs.Band;

public class BandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
