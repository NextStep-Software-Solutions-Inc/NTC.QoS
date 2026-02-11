using System.Drawing;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Domain.Models.DTOs.NAP;

public class CreateNAPDto
{
    public Guid ServiceProviderId { get; set; }
    public string Address { get; set; } = string.Empty;
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
}
