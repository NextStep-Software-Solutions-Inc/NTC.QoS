using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Aggregates;

public class StudyNetwork : BaseEntity
{
    public Guid StudyId { get; set; }
    public virtual Study? Study { get; set; }
    public Guid NetworkId { get; set; }
    public virtual Network? Network { get; set; }
}
