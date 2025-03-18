namespace NTC.QoS.Domain.Entities;
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<User> Users { get; set; } = new();
}
