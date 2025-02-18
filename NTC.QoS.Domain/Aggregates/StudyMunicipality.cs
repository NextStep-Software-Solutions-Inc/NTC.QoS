using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Aggregates;

public class StudyMunicipality : BaseEntity
{
    public Guid StudyId { get; set; }
    public virtual Study? Study { get; set; }
    public Guid MunicipalityId { get; set; }
    public virtual Municipality? Municipality { get; set; }
}
