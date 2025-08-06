using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class User : BaseEntity
{
    public Guid RoleId { get; set; }
    public virtual Role? Role { get; set; }
    public Guid OrganizationId { get; set; }
    public virtual Organization? Organization { get; set; }
    public string AuthUserId { get; set; } = string.Empty;
    public virtual List<UserRegion> UserRegions { get; set; } = new();
}
