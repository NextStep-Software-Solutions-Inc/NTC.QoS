namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class GenerationSpeedtestDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}
