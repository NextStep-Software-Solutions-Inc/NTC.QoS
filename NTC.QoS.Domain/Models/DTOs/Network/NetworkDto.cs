using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Domain.Models.DTOs.Network;

public class NetworkDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public NetworkTemplateDto? NetworkTemplate { get; set; }
    public required string SystemTypeName { get; set; }
    public required ServiceProviderDto ServiceProvider { get; set; }
    public decimal ULCenterFreq { get; set; }
    public decimal DLCenterFreq { get; set; }
    public decimal Bandwidth { get; set; }
    public LinkNetworkDto? UpLink { get; set; }
    public LinkNetworkDto? DownLink { get; set; }
}

