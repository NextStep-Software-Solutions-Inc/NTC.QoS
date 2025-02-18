namespace NTC.QoS.Domain.Entities;
public class NetworkTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string URL { get; set; } = string.Empty;
    public virtual ICollection<Network>? Networks { get; set; }

}
