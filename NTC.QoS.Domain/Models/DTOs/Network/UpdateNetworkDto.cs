namespace NTC.QoS.Domain.Models.DTOs.Network;

public class UpdateNetworkDto
{
    public string Name { get; set; } = string.Empty;
    public Enum SystemType { get; set; } = null!;
    public Guid UplinkId { get; set; }
    public Guid DownLinkId { get; set; }
}
