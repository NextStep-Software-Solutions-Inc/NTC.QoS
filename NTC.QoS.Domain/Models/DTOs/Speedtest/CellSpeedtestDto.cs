namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class CellSpeedtestDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Group { get; set; }
}
