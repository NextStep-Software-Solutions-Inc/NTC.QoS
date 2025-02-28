using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Network;

public class NetworkDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid NetworkTemplateId { get; set; }
    public virtual NetworkTemplateDto NetworkTemplate { get; set; } = new();
    public string SystemType { get; set; } = string.Empty;
    public Guid UplinkId { get; set; }
    public virtual LinkDto Uplink { get; set; } = new();
    public Guid DownLinkId { get; set; }
    public virtual LinkDto DownLink { get; set; } = new();
}
public class NetworkTemplateDto
{
    public string Name { get; set; } = string.Empty;
}
public class LinkDto
{
    public string Name { get; set; } = string.Empty;
}
