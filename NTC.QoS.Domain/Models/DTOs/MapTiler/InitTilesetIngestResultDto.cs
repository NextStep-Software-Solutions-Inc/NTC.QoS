namespace NTC.QoS.Domain.Models.DTOs.MapTiler;

/// <summary>
/// Result returned to client containing direct upload URL and ingest session IDs
/// </summary>
public class InitTilesetIngestResultDto
{
    /// <summary>
    /// Database record ID for tracking
    /// </summary>
    public Guid TilesetId { get; set; }

    /// <summary>
    /// MapTiler Ingest Task UUID
    /// </summary>
    public string IngestId { get; set; } = string.Empty;

    /// <summary>
    /// Direct presigned upload URL where client PUTs binary chunks
    /// </summary>
    public string UploadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Current ingestion state (e.g., "upload")
    /// </summary>
    public string State { get; set; } = "upload";
}
