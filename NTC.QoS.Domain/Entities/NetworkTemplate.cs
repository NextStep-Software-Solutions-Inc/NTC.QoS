namespace NTC.QoS.Domain.Entities;
public class NetworkTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string URL { get; set; } = string.Empty;
    public virtual List<Network> Networks { get; set; } = new();
}
