using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Commands;

public class ProcessTilesetIngestHandler(
    IRepository<CellularCoverageTileset> tilesetRepository,
    IMapTilerIngestService mapTilerService,
    IBackgroundJobService backgroundJobService,
    ILogger<ProcessTilesetIngestHandler> logger)
    : IRequestHandler<ProcessTilesetIngestCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        ProcessTilesetIngestCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tileset = await tilesetRepository
                .Query(t => t.Id == request.TilesetId)
                .FirstOrDefaultAsync(cancellationToken);

            if (tileset == null)
            {
                return new ApiResponse<bool>
                {
                    Succeeded = false,
                    Message = $"Tileset with Id {request.TilesetId} not found",
                    Data = false
                };
            }

            logger.LogInformation("Notifying MapTiler to process IngestId {IngestId} for Tileset {TilesetId}", tileset.IngestId, tileset.Id);

            var processResult = await mapTilerService.ProcessIngestAsync(tileset.IngestId, request.UploadResult, cancellationToken);
            if (processResult == null)
            {
                logger.LogWarning("MapTiler ProcessIngest returned null for IngestId {IngestId}", tileset.IngestId);
            }

            tileset.Status = TilesetStatusEnum.Processing;
            tilesetRepository.Update(tileset);
            await tilesetRepository.SaveChangesAsync(cancellationToken);

            // Schedule background polling job to track MapTiler ingestion to completion
            backgroundJobService.Schedule<IPollMapTilerTilesetJob>(
                job => job.ExecuteAsync(tileset.Id, tileset.IngestId, 1, CancellationToken.None),
                TimeSpan.FromSeconds(5));

            logger.LogInformation("Background job scheduled to poll MapTiler ingest for Tileset {TilesetId}", tileset.Id);

            return new ApiResponse<bool>
            {
                Succeeded = true,
                Message = "File upload completed. MapTiler tileset generation has been queued.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting tileset ingest processing for TilesetId {TilesetId}", request.TilesetId);
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = $"Error starting tileset processing: {ex.Message}",
                Data = false
            };
        }
    }
}
