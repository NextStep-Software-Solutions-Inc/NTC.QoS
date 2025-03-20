using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Domain.Models.DTOs.NAP;

public class NAPDto
{
    public Guid Id { get; set; }
    public Guid ServiceProviderId { get; set; }
    public ServiceProviderNAPDto? ServiceProvider { get; set; }
    public Guid? FileId { get; set; }
    public FileDto? File { get; set; }
    public Guid? ZoneId { get; set; }
    public ZoneNAPDto? Zone { get; set; }
    public required string CoorLatitude { get; set; }
    public required string CoorLongitude { get; set; }
}
