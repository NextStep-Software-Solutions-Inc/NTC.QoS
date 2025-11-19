using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class User : BaseEntity
{
    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;
    public string AuthUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public virtual List<UserRegion> UserRegions { get; set; } = [];
}
