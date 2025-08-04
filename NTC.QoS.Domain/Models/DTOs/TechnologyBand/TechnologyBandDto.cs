using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Domain.Models.DTOs.TechnologyBand;

public class TechnologyBandDto
{
    public Guid Id { get; set; }
    public BandDto? Band { get; set; }
    public TechnologyDto? Technology { get; set; }
}