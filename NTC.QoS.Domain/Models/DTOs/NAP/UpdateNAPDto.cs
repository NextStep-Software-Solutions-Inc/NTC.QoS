using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.NAP;

public class UpdateNAPDto
{
    public Guid? ServiceProviderId { get; set; }
    public string? Address { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? Latitude { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? Longitude { get; set; }
}
