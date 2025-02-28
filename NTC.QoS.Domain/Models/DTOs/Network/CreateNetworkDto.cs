namespace NTC.QoS.Domain.Models.DTOs.Network;

public class CreateNetworkDto
{
    public string Name { get; set; } = string.Empty;
    public Guid NetworkTemplateId { get; set; }
    public Enum SystemType { get; set; } = null!;
    public Guid UplinkId { get; set; }
    public Guid DownLinkId { get; set; }
}
