namespace NTC.QoS.Domain.Models.DTOs.Network;

public class CreateNetworkDto
{
    public required string Name { get; set; }
    public required Guid NetworkTemplateId { get; set; }
    public required Guid SystemTypeId { get; set; }
    public required Guid UplinkId { get; set; }
    public required Guid DownLinkId { get; set; }
}
