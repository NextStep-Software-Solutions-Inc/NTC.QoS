using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Entities;

public class Network : BaseEntity
{
    public Guid NetworkTemplateId { get; set; }
    public virtual NetworkTemplate? NetworkTemplate { get; set; }
    public string Name { get; set; } = string.Empty;
    public NetworkSystemTypeEnum Type { get; set; } = NetworkSystemTypeEnum.Unknown;
    public Guid UpLinkId { get; set; }
    public virtual Link? UpLink { get; set; }
    public Guid DownLinkId { get; set; }
    public virtual Link? DownLink { get; set; }
    public virtual ICollection<StudyNetwork>? StudyNetworks { get; set; }
    public virtual ICollection<Sector>? Sectors { get; set; }
}
