namespace NTC.QoS.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether this role is a protected system role (e.g. super_admin, regional_admin).
    /// System roles can only be edited or deleted by a Super Admin.
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// Role scope: "Platform" (global) or "Regional" (scoped to specific region).
    /// </summary>
    public string Scope { get; set; } = "Regional";

    /// <summary>
    /// If Scope == "Regional", specifies the Region to which this role is bound.
    /// Null for Platform / National roles.
    /// </summary>
    public Guid? RegionId { get; set; }
    public virtual Region? Region { get; set; }

    /// <summary>
    /// List of permission keys assigned to this role (stored natively as PostgreSQL text[]).
    /// </summary>
    public List<string> Permissions { get; set; } = [];

    public virtual List<User> Users { get; set; } = [];
}
