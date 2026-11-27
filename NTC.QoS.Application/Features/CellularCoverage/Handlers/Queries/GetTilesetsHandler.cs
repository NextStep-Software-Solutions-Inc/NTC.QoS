using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MapTiler;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Queries;

public class GetTilesetsHandler(
    IRepository<CellularCoverageTileset> tilesetRepository)
    : IRequestHandler<GetTilesetsQuery, ApiResponse<List<CellularCoverageTilesetDto>>>
{
    public async Task<ApiResponse<List<CellularCoverageTilesetDto>>> Handle(
        GetTilesetsQuery request,
        CancellationToken cancellationToken)
    {
        var query = tilesetRepository
            .Query(_ => true)
            .Include(t => t.Region)
            .Include(t => t.ServiceProvider)
                .ThenInclude(sp => sp!.Provider)
            .Include(t => t.Band)
            .Include(t => t.Technology)
            .AsNoTracking();

        if (request.RegionId.HasValue)
        {
            query = query.Where(t => t.RegionId == request.RegionId);
        }

        var tilesets = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = tilesets.Select(t => new CellularCoverageTilesetDto
        {
            Id = t.Id,
            Name = t.Name,
            FileName = t.FileName,
            FileSize = t.FileSize,
            IngestId = t.IngestId,
            DocumentId = t.DocumentId,
            TileJsonUrl = t.TileJsonUrl,
            Status = t.Status,
            ErrorMessage = t.ErrorMessage,
            RegionId = t.RegionId,
            RegionName = t.Region?.Name,
            ServiceProviderId = t.ServiceProviderId,
            ServiceProviderName = t.ServiceProvider?.Provider?.Name,
            BandId = t.BandId,
            BandName = t.Band?.Name,
            TechnologyId = t.TechnologyId,
            TechnologyName = t.Technology?.Standard,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt
        }).ToList();

        return new ApiResponse<List<CellularCoverageTilesetDto>>
        {
            Succeeded = true,
            Message = "Tilesets retrieved successfully",
            Data = dtos
        };
    }
}
