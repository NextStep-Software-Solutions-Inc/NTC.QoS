using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Domain.Models.DTOs.GenerationBand;

public class GenerationBandDto
{
    public Guid Id { get; set; }
    public GenerationDto? Generation { get; set; }
    public BandDto? Band { get; set; }
}
