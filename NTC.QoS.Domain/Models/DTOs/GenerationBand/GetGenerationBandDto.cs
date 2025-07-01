namespace NTC.QoS.Domain.Models.DTOs.GenerationBand;

public class GetGenerationBandDto
{
    public Guid Id { get; set; }
    public GenerationBandDto? Generation { get; set; }
    public GenerationBandDto? Band { get; set; }
}
