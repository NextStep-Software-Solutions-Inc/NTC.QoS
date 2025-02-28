using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Network;

public class NetworkDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid NetworkTemplateId { get; set; }
    public virtual NetworkTemplateDto NetworkTemplate { get; set; } = new();
    public Enum SystemType { get; set; } = NetworkSystemTypeEnum.Unknown;
    public virtual UplinkDto UpLink { get; set; } = new(); 
    public Guid UplinkId { get; set; }
    public virtual DownLinkDto DownLink { get; set; } = new();
    public Guid DownLinkId { get; set; }
}
public class NetworkTemplateDto
{
    public string Name { get; set; } = string.Empty;
}
public class DownLinkDto
{
    public string Name { get; set; } = string.Empty;
}
public class UplinkDto
{
    public string Name { get; set; } = string.Empty;
}
