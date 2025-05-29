using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Aggregates;

public class Network : BaseEntity
{
    public Guid NetworkTemplateId { get; set; }
    public virtual NetworkTemplate? NetworkTemplate { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid SystemTypeId { get; set; }
    public virtual SystemType? SystemType { get; set; }
    public Guid UpLinkId { get; set; }
    public virtual Link? UpLink { get; set; }
    public Guid DownLinkId { get; set; }
    public virtual Link? DownLink { get; set; }
    public virtual List<StudyNetwork> StudyNetworks { get; set; } = [];
    public virtual List<Sector> Sectors { get; set; } = [];
    public virtual List<CellularCoverage> CellularCoverages { get; set; } = [];
}
