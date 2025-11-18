namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

/// <summary>
/// Minimal DTO for viewport-based speedtest queries (v2 API).
/// OPTIMIZED FOR MILLIONS OF DATA POINTS - Contains only essential fields for map rendering.
/// No Guid ID field to save ~16 bytes per record.
/// </summary>
public class SpeedtestViewportDto
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
    /// Download speed in Mbps
    /// </summary>
    public decimal DownloadSpeed { get; set; }

    /// <summary>
    /// Upload speed in Mbps
    /// </summary>
    public decimal UploadSpeed { get; set; }

    /// <summary>
    /// Signal quality (e.g., "Good", "Fair", "Poor")
    /// </summary>
    public string? SignalQuality { get; set; }
}
