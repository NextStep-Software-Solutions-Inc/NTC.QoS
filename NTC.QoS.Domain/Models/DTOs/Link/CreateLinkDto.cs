using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Link;

public class CreateLinkDto
{
    public LinkTypeEnum Type { get; set; }
    public float? CenterFrequency { get; set; }
    public float? LowerFrequency { get; set; }
    public float? UpperFrequency { get; set; }
    public float? RxThreshold { get; set; }
    public Guid BandId { get; set; }
    public Guid ServiceProviderId { get; set; }
}
