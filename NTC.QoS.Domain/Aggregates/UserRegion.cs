using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Aggregates;

public class UserRegion : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public Guid RegionId { get; set; }
    public virtual Region Region { get; set; } = null!;
    public string Status { get; set; } = UserRegionStatus.Pending.ToString();
}
