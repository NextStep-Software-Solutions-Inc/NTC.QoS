using System.Text.Json.Serialization;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Domain.Models.DTOs.NAP;

public class NAPDto
{
    public Guid Id { get; set; }

    [JsonPropertyName("ISP")]
    public required string ISP { get; set; }

    [JsonPropertyName("ISPColor")]
    public required string ISPColor { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Dist { get; set; }
}
