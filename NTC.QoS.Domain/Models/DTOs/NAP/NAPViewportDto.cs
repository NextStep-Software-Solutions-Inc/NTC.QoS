using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.NAP;

/// <summary>
/// Lightweight DTO for viewport-based NAP queries (v2 API).
/// Optimized for map rendering - excludes Guid ID that UI doesn't need.
/// Reduces payload size when returning thousands of NAPs in viewport.
/// </summary>
public class NAPViewportDto
{
    /// <summary>
    /// Internet Service Provider name
    /// </summary>
    [JsonPropertyName("ISP")]
    public required string ISP { get; set; }

    /// <summary>
    /// ISP brand color for map marker styling
    /// </summary>
    [JsonPropertyName("ISPColor")]
    public required string ISPColor { get; set; }

    /// <summary>
    /// Latitude coordinate (WGS84)
    /// </summary>
    [JsonPropertyName("Lat")]
    public double Lat { get; set; }

    /// <summary>
    /// Longitude coordinate (WGS84)
    /// </summary>
    [JsonPropertyName("Lng")]
    public double Lng { get; set; }
}
