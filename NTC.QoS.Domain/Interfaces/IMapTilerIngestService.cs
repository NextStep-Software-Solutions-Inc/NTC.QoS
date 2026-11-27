using NTC.QoS.Domain.Models.DTOs.MapTiler;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service interface for interacting with MapTiler Cloud Service / Ingest API
/// </summary>
public interface IMapTilerIngestService
{
    /// <summary>
    /// Optional configured default MapTiler document_id used for single-tileset replace mode.
    /// Null when running in multi-tileset mode.
    /// </summary>
    string? DefaultDocumentId { get; }

    /// <summary>
    /// Initializes a dataset ingestion task on MapTiler Cloud.
    /// When targetDocumentId (or DefaultDocumentId) is provided, replaces that existing tileset in place.
    /// </summary>
    Task<MapTilerIngestResponseDto?> InitIngestAsync(
        string fileName,
        long fileSize,
        string? targetDocumentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams binary file content to the MapTiler presigned upload URL server-to-server (avoids browser CORS issues)
    /// </summary>
    Task<bool> UploadBinaryAsync(
        string uploadUrl,
        Stream content,
        long? contentLength,
        CancellationToken cancellationToken = default);

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
    /// Constructs the TileJSON URL for a published MapTiler tileset, with optional cache-busting token
    /// </summary>
    string GetTileJsonUrl(string documentId, string? cacheBuster = null);
}
