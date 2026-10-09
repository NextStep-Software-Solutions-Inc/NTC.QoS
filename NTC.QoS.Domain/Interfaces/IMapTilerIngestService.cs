using NTC.QoS.Domain.Models.DTOs.MapTiler;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service interface for interacting with MapTiler Cloud Service / Ingest API
/// </summary>
public interface IMapTilerIngestService
{
    /// <summary>
    /// Initializes a dataset ingestion task on MapTiler Cloud
    /// </summary>
    Task<MapTilerIngestResponseDto?> InitIngestAsync(string fileName, long fileSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers processing on MapTiler Cloud after file upload completes
    /// </summary>
    Task<MapTilerIngestResponseDto?> ProcessIngestAsync(string ingestId, object? uploadResult = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves current state, progress, and document ID for an ongoing ingest task
    /// </summary>
    Task<MapTilerIngestResponseDto?> GetIngestDetailsAsync(string ingestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an in-progress dataset ingestion
    /// </summary>
    Task<bool> CancelIngestAsync(string ingestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Constructs the TileJSON URL for a published MapTiler tileset
    /// </summary>
    string GetTileJsonUrl(string documentId);
}
