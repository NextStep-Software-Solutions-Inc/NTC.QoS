namespace NTC.QoS.Domain.Models.DTOs.Cell;

/// <summary>
/// Minimal DTO for viewport-based cell queries (v2 API).
/// OPTIMIZED FOR MILLIONS OF DATA POINTS - Contains only essential fields for map rendering.
/// No Guid ID field to save ~16 bytes per record.
/// </summary>
public class CellViewportDto
{
    /// <summary>
    /// Service provider name (e.g., "Globe", "Smart")
    /// </summary>
    public string ISP { get; set; } = string.Empty;

    /// <summary>
    /// Provider color for map rendering (hex color code)
    /// </summary>
    public string ISPColor { get; set; } = string.Empty;

    /// <summary>
    /// Latitude coordinate (Y)
    /// </summary>
    public double Lat { get; set; }

    /// <summary>
    /// Longitude coordinate (X)
    /// </summary>
    public double Lng { get; set; }

    /// <summary>
    /// Cell name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cell status (e.g., "Active", "Inactive")
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
