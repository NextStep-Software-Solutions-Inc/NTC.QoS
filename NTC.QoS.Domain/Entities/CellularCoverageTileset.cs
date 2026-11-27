using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// CellularCoverageTileset - MapTiler vector tileset metadata for Cellular Coverage (RP3) outputs
/// </summary>
public class CellularCoverageTileset : BaseEntity
{
    /// <summary>
    /// User-friendly label or display title (e.g. "Smart 4G Coverage Q1 2026")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Original file name uploaded
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MapTiler Ingest Task UUID
    /// </summary>
    public string IngestId { get; set; } = string.Empty;

    /// <summary>
    /// MapTiler permanent document/tileset ID assigned after processing
    /// </summary>
    public string? DocumentId { get; set; }

    /// <summary>
    /// Direct TileJSON URL consumed by MapTiler SDK / MapLibre:
    /// https://api.maptiler.com/tiles/{DocumentId}/tiles.json?key={ApiKey}
    /// </summary>
    public string? TileJsonUrl { get; set; }

    /// <summary>
    /// Ingestion state
    /// </summary>
    public TilesetStatusEnum Status { get; set; } = TilesetStatusEnum.Pending;

    /// <summary>
    /// Error message if ingestion fails
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Associated Region
    /// </summary>
    public Guid? RegionId { get; set; }
    public virtual Region? Region { get; set; }

    /// <summary>
    /// Associated Service Provider
    /// </summary>
    public Guid? ServiceProviderId { get; set; }
    public virtual ServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// Associated Band (optional)
    /// </summary>
    public Guid? BandId { get; set; }
    public virtual Band? Band { get; set; }

    /// <summary>
    /// Associated Technology (optional)
    /// </summary>
    public Guid? TechnologyId { get; set; }
    public virtual Technology? Technology { get; set; }

    /// <summary>
    /// Flag indicating if this tileset is active for display
    /// </summary>
    public bool IsActive { get; set; } = true;
}
