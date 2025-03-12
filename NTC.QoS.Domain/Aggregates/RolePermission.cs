using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Aggregates;
public class RolePermission : BaseEntity
{
    public Guid PermissionId { get; set; }
    public virtual Permission? Permission { get; set; }
    public Guid RoleId { get; set; }
    public virtual Role? Role { get; set; }
}
