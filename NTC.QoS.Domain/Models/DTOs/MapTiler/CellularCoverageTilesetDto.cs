using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.MapTiler;

/// <summary>
/// DTO representing a registered Cellular Coverage Tileset
/// </summary>
public class CellularCoverageTilesetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string IngestId { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
    public string? TileJsonUrl { get; set; }
    public TilesetStatusEnum Status { get; set; }
    public string StatusText => Status.ToString();
    public string? ErrorMessage { get; set; }

    public Guid? RegionId { get; set; }
    public string? RegionName { get; set; }

    public Guid? ServiceProviderId { get; set; }
    public string? ServiceProviderName { get; set; }

    public Guid? BandId { get; set; }
    public string? BandName { get; set; }

    public Guid? TechnologyId { get; set; }
    public string? TechnologyName { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
