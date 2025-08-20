namespace NTC.QoS.Domain.Models.DTOs.Network;

public class UpdateNetworkDto
{
    public string? Name { get; set; }
    public decimal? ULCenterFreq { get; set; }
    public decimal? DLCenterFreq { get; set; }
    public decimal? Bandwidth { get; set; }
}
