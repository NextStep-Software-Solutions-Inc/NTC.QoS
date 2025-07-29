namespace NTC.QoS.Domain.Models.DTOs.Link;

public class CreateLinkDto
{
    public required string Type { get; set; }
    public decimal CenterFrequency { get; set; }
    public decimal LowerFrequency { get; set; }
    public decimal UpperFrequency { get; set; }
    public decimal RxThreshold { get; set; }
    public Guid TechnologyBandId { get; set; }
    public Guid ServiceProviderId { get; set; }
}
