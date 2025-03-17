namespace NTC.QoS.Domain.Models.DTOs.NAP;

public class NAPDto
{
    public Guid Id { get; set; }
    public virtual ServiceProviderNAPDto? ServiceProvider { get; set; }
    public Guid? FileId { get; set; }
    public virtual ZoneNAPDto? Zone { get; set; }
    public required string CoorLatitude { get; set; }
    public required string CoorLongitude { get; set; }
}
