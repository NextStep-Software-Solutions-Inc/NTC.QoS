using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Link;

public class LinkDto
{
    public Guid Id { get; set; }
    public Guid ServiceProviderId { get; set; }
    public Guid BandId { get; set; }
    public string BandName { get; set; } = string.Empty;
    public LinkTypeEnum Type { get; set; }
    public decimal CenterFrequency { get; set; }
    public decimal LowerFrequency { get; set; }
    public decimal UpperFrequency { get; set; }
    public decimal RxThreshold { get; set; }
}
