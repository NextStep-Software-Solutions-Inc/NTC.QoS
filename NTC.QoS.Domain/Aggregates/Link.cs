using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Aggregates;

public class Link : BaseEntity
{
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider ServiceProvider { get; set; } = null!;
    public LinkTypeEnum Type { get; set; } = LinkTypeEnum.Unknown;
    public Guid BandId { get; set; }
    public virtual Band Band { get; set; } = null!;
    public decimal CenterFrequency { get; set; }
    public decimal LowerFrequency { get; set; }
    public decimal UpperFrequency { get; set; }
    public decimal RxThreshold { get; set; }
    public virtual List<Network> UpLinks { get; set; } = new();
    public virtual List<Network> DownLinks { get; set; } = new();
}
