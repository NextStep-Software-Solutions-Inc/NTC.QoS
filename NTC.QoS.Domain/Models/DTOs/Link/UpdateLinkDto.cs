namespace NTC.QoS.Domain.Models.DTOs.Link;

public class UpdateLinkDto
{
    public string? Type { get; set; }
    public decimal? CenterFrequency { get; set; }
    public decimal? LowerFrequency { get; set; }
    public decimal? UpperFrequency { get; set; }
    public decimal? RxThreshold { get; set; }
}
