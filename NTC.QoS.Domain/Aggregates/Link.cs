using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Entities;

public class Link : BaseEntity
{
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider? ServiceProvider { get; set; }
    public LinkTypeEnum Type { get; set; } = LinkTypeEnum.Unknown;
    public Guid BandId { get; set; }
    public virtual Band? Band { get; set; }
    public decimal CenterFrequencey { get; set; }
    public decimal LowerFrequencey { get; set; }
    public decimal UpperFrequencey { get; set; }
    public decimal RxThreshold { get; set; }



}
