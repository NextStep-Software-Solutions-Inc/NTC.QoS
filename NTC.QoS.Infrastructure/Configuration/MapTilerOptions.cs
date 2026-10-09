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
}
