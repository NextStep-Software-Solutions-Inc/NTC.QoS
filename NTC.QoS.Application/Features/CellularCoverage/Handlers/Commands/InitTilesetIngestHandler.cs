using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MapTiler;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Commands;

public class InitTilesetIngestHandler(
    IRepository<CellularCoverageTileset> tilesetRepository,
    IMapTilerIngestService mapTilerService,
    IUserContextService userContextService,
    ILogger<InitTilesetIngestHandler> logger)
    : IRequestHandler<InitTilesetIngestCommand, ApiResponse<InitTilesetIngestResultDto>>
{
    public async Task<ApiResponse<InitTilesetIngestResultDto>> Handle(
        InitTilesetIngestCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Initiating MapTiler ingest session for file {FileName} ({Size} bytes)", request.FileName, request.FileSize);

            var ingestRes = await mapTilerService.InitIngestAsync(request.FileName, request.FileSize, cancellationToken);
            if (ingestRes == null || string.IsNullOrWhiteSpace(ingestRes.UploadUrl))
            {
                return new ApiResponse<InitTilesetIngestResultDto>
                {
                    Succeeded = false,
                    Message = "Failed to initiate MapTiler ingestion session. Please verify MapTiler configuration and token permissions.",
                    Data = null
                };
            }

            var tileset = new CellularCoverageTileset
            {
                Name = !string.IsNullOrWhiteSpace(request.DisplayName)
                    ? request.DisplayName
                    : Path.GetFileNameWithoutExtension(request.FileName),
                FileName = request.FileName,
                FileSize = request.FileSize,
                IngestId = ingestRes.Id,
                Status = TilesetStatusEnum.Uploading,
                RegionId = request.RegionId,
                ServiceProviderId = request.ServiceProviderId,
                BandId = request.BandId,
                TechnologyId = request.TechnologyId,
                CreatedBy = userContextService.GetCurrentUserId() ?? "System",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await tilesetRepository.AddAsync(tileset, cancellationToken);
            await tilesetRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Tileset record created with Id {TilesetId}, MapTiler IngestId {IngestId}", tileset.Id, ingestRes.Id);

            return new ApiResponse<InitTilesetIngestResultDto>
            {
                Succeeded = true,
                Message = "MapTiler ingestion session created. You may now upload binary chunks to the provided upload URL.",
                Data = new InitTilesetIngestResultDto
                {
                    TilesetId = tileset.Id,
                    IngestId = ingestRes.Id,
                    UploadUrl = ingestRes.UploadUrl,
                    State = ingestRes.State
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initiating tileset ingest for {FileName}", request.FileName);
            return new ApiResponse<InitTilesetIngestResultDto>
            {
                Succeeded = false,
                Message = $"Error initiating tileset ingestion: {ex.Message}",
                Data = null
            };
        }
    }
}
