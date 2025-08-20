using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.CellSector;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class CellularDto
{
    public Guid Id { get; set; }
    public CellSectorDto? CellSector { get; set; }
    public ServiceProviderDto? ServiceProvider { get; set; }
    public TechnologyDto? Technology { get; set; }
    public BandDto? Band { get; set; }
    public string? Address { get; set; }
}
