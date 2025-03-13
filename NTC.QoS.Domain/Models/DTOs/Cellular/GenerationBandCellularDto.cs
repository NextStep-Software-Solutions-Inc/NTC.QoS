namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class GenerationBandCellularDto
{
    public Guid Id { get; set; }
    public CommonCellularDto? Generation { get; set; }
    public CommonCellularDto? Band { get; set; }
}
