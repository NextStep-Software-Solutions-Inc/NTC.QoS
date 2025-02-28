using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Network;

public class NetworkDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid NetworkTemplateId { get; set; }
    public Enum SystemType { get; set; } = NetworkSystemTypeEnum.Unknown;
    public Guid UplinkId { get; set; }
    public Guid DownLinkId { get; set; }
}
