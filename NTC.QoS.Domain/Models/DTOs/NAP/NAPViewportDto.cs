using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.NAP;

/// <summary>
/// DTO for viewport-based NAP queries (v2 API).
/// Contains all fields required for map rendering, inline editing, and deletion.
/// </summary>
public class NAPViewportDto
{
    /// <summary>
    /// NAP database identifier – required by the FE to execute edit (PATCH) and delete operations.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Internet Service Provider name
    /// </summary>
    public required string ISP { get; set; }

    /// <summary>
    /// ISP brand color for map marker styling
    /// </summary>
    public required string ISPColor { get; set; }

    /// <summary>
    /// Service provider identifier – required to pre-populate the edit form.
    /// </summary>
    public Guid ServiceProviderId { get; set; }

    /// <summary>
    /// Street / location address – used for display and edit form pre-population.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Latitude coordinate (WGS84)
    /// </summary>
    public double Lat { get; set; }

    /// <summary>
    /// Longitude coordinate (WGS84)
    /// </summary>
    public double Lng { get; set; }
}
