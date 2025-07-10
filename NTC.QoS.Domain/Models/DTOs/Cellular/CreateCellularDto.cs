namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class CreateCellularDto
{
    public Guid CellSectorId { get; set; }
    public Guid ServiceProviderId { get; set; }
    public Guid GenerationBandId { get; set; }
    public string? Address { get; set; }
}
