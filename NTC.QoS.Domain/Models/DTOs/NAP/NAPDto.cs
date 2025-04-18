using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Domain.Models.DTOs.NAP;

public class NAPDto
{
    public Guid Id { get; set; }
    public required ServiceProviderDto ServiceProvider { get; set; }
    public virtual FileDto? File { get; set; }
    public virtual ZoneDto? Zone { get; set; }
    public required string CoorLatitude { get; set; }
    public required string CoorLongitude { get; set; }
}
