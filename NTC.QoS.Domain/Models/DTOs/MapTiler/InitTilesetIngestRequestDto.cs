using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.Domain.Models.DTOs.MapTiler;

/// <summary>
/// Request to initiate a direct MapTiler ingest session for Cellular Coverage MBTiles
/// </summary>
public class InitTilesetIngestRequestDto
{
    [Required]
    public string FileName { get; set; } = string.Empty;

    [Range(1, long.MaxValue, ErrorMessage = "File size must be greater than 0")]
    public long FileSize { get; set; }

    public string? DisplayName { get; set; }

    public Guid? RegionId { get; set; }

    public Guid? ServiceProviderId { get; set; }

    public Guid? BandId { get; set; }

    public Guid? TechnologyId { get; set; }
}
