namespace NTC.QoS.Domain.Models.DTOs.Network;

public class CreateNetworkDto
{
    public required string Name { get; set; }
    public Guid? NetworkTemplateId { get; set; }
    public required Guid SystemTypeId { get; set; }
    public required Guid ServiceProviderId { get; set; }
    public required Guid BandId { get; set; }
    public Guid? MunicipalityId { get; set; }
    public decimal ULCenterFreq { get; set; }
    public decimal DLCenterFreq { get; set; }
    public decimal Bandwidth { get; set; }
}
