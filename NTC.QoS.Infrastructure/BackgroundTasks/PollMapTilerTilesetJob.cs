using System.Text.Json;
using System.Text.RegularExpressions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Configuration;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Hangfire background job to poll MapTiler Cloud ingestion progress and finalize tileset status
/// </summary>
public class PollMapTilerTilesetJob : IPollMapTilerTilesetJob
{
    private readonly IRepository<CellularCoverageTileset> _tilesetRepository;
    private readonly IMapTilerIngestService _mapTilerService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<PollMapTilerTilesetJob> _logger;
    private readonly int _maxAttempts;
    private readonly TimeSpan _pollInterval;

    public PollMapTilerTilesetJob(
        IRepository<CellularCoverageTileset> tilesetRepository,
        IMapTilerIngestService mapTilerService,
        IBackgroundJobService backgroundJobService,
        ILogger<PollMapTilerTilesetJob> logger,
        IOptions<MapTilerOptions>? options = null)
    {
        _tilesetRepository = tilesetRepository;
        _mapTilerService = mapTilerService;
        _backgroundJobService = backgroundJobService;
        _logger = logger;

        var mapTilerOptions = options?.Value ?? new MapTilerOptions();
        _maxAttempts = Math.Max(1, mapTilerOptions.PollMaxAttempts);
        _pollInterval = TimeSpan.FromSeconds(Math.Max(5, mapTilerOptions.PollIntervalSeconds));
    }

    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [SucceededJobExpiration(expirationMinutes: 60)]
    public async Task ExecuteAsync(
        Guid tilesetId,
        string ingestId,
        int attemptCount,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Polling MapTiler ingest for Tileset {TilesetId}, IngestId {IngestId} (Attempt {Attempt}/{MaxAttempts})",
            tilesetId, ingestId, attemptCount, _maxAttempts);

        var tileset = await _tilesetRepository
            .Query(t => t.Id == tilesetId)
            .FirstOrDefaultAsync(cancellationToken);

        if (tileset == null)
        {
            _logger.LogWarning("Tileset record {TilesetId} not found. Aborting poll job.", tilesetId);
            return;
        }

        var details = await _mapTilerService.GetIngestDetailsAsync(ingestId, cancellationToken);
        if (details == null)
        {
            _logger.LogWarning("Failed to fetch ingest details from MapTiler for {IngestId}", ingestId);
            await RescheduleOrTimeoutAsync(tilesetId, ingestId, attemptCount, tileset, cancellationToken);
            return;
        }

        var state = details.State?.ToLowerInvariant();
        _logger.LogInformation(
            "MapTiler Ingest {IngestId} state: {State}, Progress: {Progress}%",
            ingestId, state, details.Progress);

        var validDetailsDocumentId = !string.IsNullOrWhiteSpace(details.DocumentId) && details.DocumentId != Guid.Empty.ToString()
            ? details.DocumentId
            : null;
        var isInProgress = state is "upload" or "processing" or "queued";
        var isCompleted = state == "completed" || (!isInProgress && string.IsNullOrWhiteSpace(state) && validDetailsDocumentId != null);

        if (isCompleted)
        {
            var documentId = validDetailsDocumentId ?? tileset.DocumentId ?? _mapTilerService.DefaultDocumentId;
            tileset.DocumentId = documentId;
            tileset.Status = TilesetStatusEnum.Ready;
            tileset.ErrorMessage = null;

            if (!string.IsNullOrWhiteSpace(documentId))
            {
                var cacheBuster = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                tileset.TileJsonUrl = _mapTilerService.GetTileJsonUrl(documentId, cacheBuster);

                // In single-tileset replace mode (or when overwriting the same DocumentId),
                // deactivate superseded active tileset records so /tilesets/active returns only the latest one.
                var isReplaceMode = !string.IsNullOrWhiteSpace(_mapTilerService.DefaultDocumentId);
                var supersededTilesets = await _tilesetRepository
                    .Query(t => t.Id != tileset.Id && t.IsActive && (isReplaceMode || t.DocumentId == documentId))
                    .ToListAsync(cancellationToken);

                foreach (var oldTileset in supersededTilesets)
                {
                    oldTileset.IsActive = false;
                    _tilesetRepository.Update(oldTileset);
                }
            }

            _tilesetRepository.Update(tileset);
            await _tilesetRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "MapTiler tileset {TilesetId} successfully completed! DocumentId: {DocumentId}, TileJsonUrl: {TileJsonUrl}",
                tilesetId, documentId, tileset.TileJsonUrl);
            return;
        }

        if (state == "error" || state == "failed" || state == "canceled")
        {
            var sanitizedError = SanitizeExternalErrorMessage(details.Errors, state);
            tileset.Status = TilesetStatusEnum.Failed;
            tileset.ErrorMessage = sanitizedError;
            _tilesetRepository.Update(tileset);
            await _tilesetRepository.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                "MapTiler ingestion failed for Tileset {TilesetId} (IngestId {IngestId}, State {State}). SanitizedError: {SanitizedError}",
                tilesetId, ingestId, state, sanitizedError);
            return;
        }

        // Still in upload or processing state
        if (tileset.Status != TilesetStatusEnum.Processing)
        {
            tileset.Status = TilesetStatusEnum.Processing;
            _tilesetRepository.Update(tileset);
            await _tilesetRepository.SaveChangesAsync(cancellationToken);
        }

        await RescheduleOrTimeoutAsync(tilesetId, ingestId, attemptCount, tileset, cancellationToken);
    }

    private async Task RescheduleOrTimeoutAsync(
        Guid tilesetId,
        string ingestId,
        int attemptCount,
        CellularCoverageTileset tileset,
        CancellationToken cancellationToken)
    {
        if (attemptCount < _maxAttempts)
        {
            _backgroundJobService.Schedule<IPollMapTilerTilesetJob>(
                job => job.ExecuteAsync(tilesetId, ingestId, attemptCount + 1, CancellationToken.None),
                _pollInterval);
        }
        else
        {
            _logger.LogError("MapTiler polling timed out after {MaxAttempts} attempts for Tileset {TilesetId}", _maxAttempts, tilesetId);
            tileset.Status = TilesetStatusEnum.Failed;
            tileset.ErrorMessage = "Ingestion polling timed out. Processing may still be running on MapTiler Cloud.";
            _tilesetRepository.Update(tileset);
            await _tilesetRepository.SaveChangesAsync(cancellationToken);
        }
    }

    internal static string SanitizeExternalErrorMessage(object? rawErrors, string? state)
    {
        var fallback = $"MapTiler ingestion ended with state: {state ?? "failed"}.";
        var extracted = ExtractErrorText(rawErrors);
        if (string.IsNullOrWhiteSpace(extracted))
        {
            return fallback;
        }

        // Strip URLs, auth/query secrets, stack trace lines, and file paths so internal metadata is never persisted
        var sanitized = Regex.Replace(extracted, @"https?://\S+", "[redacted-url]", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"\b(api[_-]?key|key|token|signature|sig|authorization|secret)\s*[:=]\s*\S+", "$1=[redacted]", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"\b(Bearer|Token)\s+[A-Za-z0-9\-._~+/]+=*", "$1 [redacted]", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"(?:[A-Za-z]:\\|/(?:var|srv|home|opt|tmp|etc|usr|app)/)\S+", "[redacted-path]", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"\s+at\s+\S+\(.*?\)", string.Empty, RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return fallback;
        }

        const int maxLength = 256;
        return sanitized.Length > maxLength ? sanitized[..(maxLength - 3)] + "..." : sanitized;
    }

    private static string? ExtractErrorText(object? rawErrors)
    {
        if (rawErrors == null) return null;

        if (rawErrors is JsonElement element)
        {
            return ExtractFromJsonElement(element);
        }

        return rawErrors.ToString();
    }

    private static string? ExtractFromJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Array:
                var parts = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    var text = ExtractFromJsonElement(item);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        parts.Add(text);
                    }
                }
                return parts.Count > 0 ? string.Join("; ", parts) : null;

            case JsonValueKind.Object:
                foreach (var propName in new[] { "message", "error", "detail", "reason" })
                {
                    if (element.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
                    {
                        return prop.GetString();
                    }
                }
                return null;

            default:
                return null;
        }
    }
}
