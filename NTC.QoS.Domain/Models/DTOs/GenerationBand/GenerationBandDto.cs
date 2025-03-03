namespace NTC.QoS.Domain.Models.DTOs.GenerationBand;

public class GenerationBandDto
{
    public Guid Id { get; set; }
    public required Guid GenerationId { get; set; }
    public required Guid BandId { get; set; }
}
