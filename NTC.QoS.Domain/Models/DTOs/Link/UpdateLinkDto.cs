namespace NTC.QoS.Domain.Models.DTOs.Link;

public class UpdateLinkDto
{
    public float? CenterFrequency { get; set; }
    public float? LowerFrequency { get; set; }
    public float? UpperFrequency { get; set; }
    public float? RxThreshold { get; set; }
    public Guid BandId { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
