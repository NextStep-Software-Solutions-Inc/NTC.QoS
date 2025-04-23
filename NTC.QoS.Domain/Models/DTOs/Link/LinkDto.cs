using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Domain.Models.DTOs.Link;

public class LinkDto
{
    public Guid Id { get; set; }
    public required ServiceProviderDto ServiceProvider { get; set; }
    public required BandDto Band { get; set; }
    public required string Type { get; set; }
    public decimal CenterFrequency { get; set; }
    public decimal LowerFrequency { get; set; }
    public decimal UpperFrequency { get; set; }
    public decimal RxThreshold { get; set; }
}
