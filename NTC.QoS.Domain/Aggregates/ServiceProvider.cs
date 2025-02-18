using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Aggregates;

public class ServiceProvider : BaseEntity
{
    public Guid ProviderId { get; set; }
    public virtual Provider? Provider { get; set; }
    public Guid ServiceId { get; set; }
    public virtual Service? Service { get; set; }
    public string MNC { get; set; } = string.Empty;
    public string MCC { get; set; } = string.Empty;
    public virtual List<Speedtest> Speedtests { get; set; } = new();
    public virtual List<Link> Links { get; set; } = new();
    public virtual List<Cellular> Cellulars { get; set; } = new();
    public virtual List<NAP> NAPs { get; set; } = new();
    public virtual List<Cell> Cells { get; set; } = new();



}
