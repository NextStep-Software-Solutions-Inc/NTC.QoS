using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Aggregates;

public class UserRegion : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public Guid RegionId { get; set; }
    public virtual Region Region { get; set; } = null!;
    public string Status { get; set; } = UserRegionStatus.Pending.ToString();
}
