using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<RolePermission> RolePermissions { get; set; } = new();
    public virtual List<User> Users { get; set; } = new();
}
