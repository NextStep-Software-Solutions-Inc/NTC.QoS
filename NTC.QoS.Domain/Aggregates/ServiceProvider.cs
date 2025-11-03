using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Aggregates;

public class ServiceProvider : BaseEntity
{
    public Guid ProviderId { get; set; }
    public virtual Provider Provider { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public virtual Service Service { get; set; } = null!;
    public string MNC { get; set; } = string.Empty;
    public string MCC { get; set; } = string.Empty;
    public virtual List<Speedtest> Speedtests { get; set; } = [];
    public virtual List<Link> Links { get; set; } = [];
    public virtual List<Cellular> Cellulars { get; set; } = [];
    public virtual List<NAP> NAPs { get; set; } = [];
    public virtual List<Cell> Cells { get; set; } = [];
    public virtual List<Network> Networks { get; set; } = [];
    public virtual List<WirelessValidation> WirelessValidations { get; set; } = [];
}
