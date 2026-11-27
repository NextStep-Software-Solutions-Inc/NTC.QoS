namespace NTC.QoS.Infrastructure.Configuration;

/// <summary>
/// Configuration options for MapTiler Cloud Service and Ingest API
/// </summary>
public class MapTilerOptions
{
    public const string SectionName = "MapTiler";

    /// <summary>
    /// MapTiler Service Token (Private key used for Admin/Ingest API)
    /// </summary>
    public string ServiceToken { get; set; } = string.Empty;

    /// <summary>
    /// MapTiler Public API Key (used for client SDK map rendering)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for MapTiler Admin/Service API (default: https://service.maptiler.com/v1)
    /// </summary>
    public string BaseUrl { get; set; } = "https://service.maptiler.com/v1";

    /// <summary>
    /// Optional existing MapTiler tileset UUID (document_id) to overwrite on every upload.
    /// Set this on single-tileset plans to replace the existing custom tileset in place;
    /// leave empty on higher-tier plans to create a new tileset per upload.
    /// </summary>
    public string? DefaultDocumentId { get; set; }

    /// <summary>
    /// Maximum number of polling attempts for MapTiler ingestion before timing out.
    /// Defaults to 120 (~30 minutes at 15-second intervals) to accommodate large MBTiles under heavy load.
    /// </summary>
    public int PollMaxAttempts { get; set; } = 120;

    /// <summary>
    /// Delay in seconds between MapTiler ingestion status polls (default: 15 seconds).
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// HTTP timeout in minutes for streaming large MBTiles uploads to MapTiler storage (default: 30 minutes).
    /// </summary>
    public int UploadTimeoutMinutes { get; set; } = 30;
}
