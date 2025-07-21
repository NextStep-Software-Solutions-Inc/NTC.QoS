using NTC.QoS.Domain.Models.DTOs.CellSector;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class CellularDto
{
    public Guid Id { get; set; }
    public CellSectorDto? CellSector { get; set; }
    public ServiceProviderDto? ServiceProvider { get; set; }
    public GenerationBandDto? GenerationBand { get; set; }
    public string? Address { get; set; }
}
