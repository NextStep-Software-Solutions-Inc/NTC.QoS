namespace NTC.QoS.Domain.Entities;

public class SignalLevel : BaseEntity
{
    public Guid AreaStudyId { get; set; }
    public virtual AreaStudy? AreaStudy { get; set; }
    public int NoOfLevels { get; set; }
    public virtual List<SignalLevelEntry> SignalLevelEntries { get; set; } = [];
}