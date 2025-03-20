using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Domain.Models.DTOs.NAP;

public class CreateNAPDto
{
    public Guid ServiceProviderId { get; set; }
    public Guid? FileId { get; set; }
    public Guid? ZoneId { get; set; }
    public required string CoorLatitude { get; set; }
    public required string CoorLongitude { get; set; }
}
