using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.Domain.Models.DTOs.TechnologyBand;

public class CreateTechnologyBandDto
{
    public Guid BandId { get; set; }
    public Guid TechnologyId { get; set; }
}