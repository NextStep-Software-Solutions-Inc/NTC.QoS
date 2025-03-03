namespace NTC.QoS.Domain.Models.DTOs.Link;

public class LinkDto
{
    public Guid Id { get; set; }
    public ServiceProviderLinkDto? ServiceProvider { get; set; }
    public BandLinkDto? Band { get; set; }
    public required string Type { get; set; }
    public decimal CenterFrequency { get; set; }
    public decimal LowerFrequency { get; set; }
    public decimal UpperFrequency { get; set; }
    public decimal RxThreshold { get; set; }
}
