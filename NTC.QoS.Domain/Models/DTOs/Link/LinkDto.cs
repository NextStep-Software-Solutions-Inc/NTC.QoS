using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Domain.Models.DTOs.Link;

public class LinkDto
{
    public Guid Id { get; set; }
    public required ServiceProviderDto ServiceProvider { get; set; }
    public Guid ServiceProviderId { get; set; }
    public required BandDto Band { get; set; }
    public Guid BandId { get; set; }
    public string BandName { get; set; } = string.Empty;
    public LinkTypeEnum Type { get; set; }
    public float? CenterFrequency { get; set; }
    public float? LowerFrequency { get; set; }
    public float? UpperFrequency { get; set; }
    public float? RxThreshold { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
