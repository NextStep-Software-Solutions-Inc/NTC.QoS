namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class CommonCellularDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}
