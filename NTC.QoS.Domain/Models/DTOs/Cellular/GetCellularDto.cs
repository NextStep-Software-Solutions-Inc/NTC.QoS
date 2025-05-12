using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class GetCellularDto
{
    public Guid Id { get; set; }
    public CellDto? Cell { get; set; }
    public ServiceProviderCellularDto? ServiceProvider { get; set; }
    public GenerationBandCellularDto? GenerationBand { get; set; }
}
