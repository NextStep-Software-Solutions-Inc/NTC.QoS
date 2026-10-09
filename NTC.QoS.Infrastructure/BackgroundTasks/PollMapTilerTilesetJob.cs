using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Hangfire background job to poll MapTiler Cloud ingestion progress and finalize tileset status
/// </summary>
public class PollMapTilerTilesetJob : IPollMapTilerTilesetJob
{
    private const int MaxAttempts = 60; // Up to ~15 minutes of polling (15s intervals)
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    private readonly IRepository<CellularCoverageTileset> _tilesetRepository;
    private readonly IMapTilerIngestService _mapTilerService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<PollMapTilerTilesetJob> _logger;

    public PollMapTilerTilesetJob(
        IRepository<CellularCoverageTileset> tilesetRepository,
        IMapTilerIngestService mapTilerService,
        IBackgroundJobService backgroundJobService,
        ILogger<PollMapTilerTilesetJob> logger)
    {
        _tilesetRepository = tilesetRepository;
        _mapTilerService = mapTilerService;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task ExecuteAsync(
        Guid tilesetId,
        string ingestId,
        int attemptCount,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Polling MapTiler ingest for Tileset {TilesetId}, IngestId {IngestId} (Attempt {Attempt}/{MaxAttempts})",
            tilesetId, ingestId, attemptCount, MaxAttempts);

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
            RescheduleOrTimeout(tilesetId, ingestId, attemptCount, tileset);
            return;
        }

        var state = details.State?.ToLowerInvariant();
        _logger.LogInformation(
            "MapTiler Ingest {IngestId} state: {State}, Progress: {Progress}%",
            ingestId, state, details.Progress);

        if (state == "completed" || !string.IsNullOrWhiteSpace(details.DocumentId))
        {
            var documentId = details.DocumentId ?? tileset.DocumentId;
            tileset.DocumentId = documentId;
            tileset.Status = TilesetStatusEnum.Ready;
            tileset.ErrorMessage = null;

            if (!string.IsNullOrWhiteSpace(documentId))
            {
                tileset.TileJsonUrl = _mapTilerService.GetTileJsonUrl(documentId);
            }

            _tilesetRepository.Update(tileset);
            await _tilesetRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "MapTiler tileset {TilesetId} successfully completed! DocumentId: {DocumentId}, TileJsonUrl: {TileJsonUrl}",
                tilesetId, documentId, tileset.TileJsonUrl);
            return;
        }

        if (state == "error" || state == "canceled")
        {
            tileset.Status = TilesetStatusEnum.Failed;
            tileset.ErrorMessage = details.Errors?.ToString() ?? $"MapTiler ingestion ended with state: {state}";
            _tilesetRepository.Update(tileset);
            await _tilesetRepository.SaveChangesAsync(cancellationToken);

            _logger.LogError("MapTiler ingestion failed for Tileset {TilesetId}: {Error}", tilesetId, tileset.ErrorMessage);
            return;
        }

        // Still in upload or processing state
        if (tileset.Status != TilesetStatusEnum.Processing)
        {
            tileset.Status = TilesetStatusEnum.Processing;
            _tilesetRepository.Update(tileset);
            await _tilesetRepository.SaveChangesAsync(cancellationToken);
        }

        RescheduleOrTimeout(tilesetId, ingestId, attemptCount, tileset);
    }

    private void RescheduleOrTimeout(
        Guid tilesetId,
        string ingestId,
        int attemptCount,
        CellularCoverageTileset tileset)
    {
        if (attemptCount < MaxAttempts)
        {
            _backgroundJobService.Schedule<IPollMapTilerTilesetJob>(
                job => job.ExecuteAsync(tilesetId, ingestId, attemptCount + 1, CancellationToken.None),
                PollInterval);
        }
        else
        {
            _logger.LogError("MapTiler polling timed out after {MaxAttempts} attempts for Tileset {TilesetId}", MaxAttempts, tilesetId);
            tileset.Status = TilesetStatusEnum.Failed;
            tileset.ErrorMessage = "Ingestion polling timed out. Processing may still be running on MapTiler Cloud.";
            _tilesetRepository.Update(tileset);
            _tilesetRepository.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
