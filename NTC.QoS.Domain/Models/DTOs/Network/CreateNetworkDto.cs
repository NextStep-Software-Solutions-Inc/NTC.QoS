namespace NTC.QoS.Domain.Models.DTOs.Network;

public class CreateNetworkDto
{
    public required string Name { get; set; }
    public Guid NetworkTemplateId { get; set; }
    public required string SystemType { get; set; }
    public Guid UplinkId { get; set; }
    public Guid DownLinkId { get; set; }
}
