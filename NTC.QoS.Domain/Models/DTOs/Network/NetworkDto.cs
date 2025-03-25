using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Domain.Models.DTOs.Network;

public class NetworkDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public NetworkTemplateDto? NetworkTemplate { get; set; }
    public required string SystemType { get; set; }
    public LinkNetworkDto? Uplink { get; set; }
    public LinkNetworkDto? DownLink { get; set; }
}
