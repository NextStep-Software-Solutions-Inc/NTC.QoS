using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;
using NTC.QoS.Infrastructure.Persistence;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Models.DTOs.Dashboard;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Services;

public class GeospatialQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public GeospatialQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets top geographic entities by cell count with optimized spatial queries.
    /// OPTIMIZED: Uses single batched query with GroupJoin to minimize database roundtrips.
    /// </summary>
    public async Task<List<GeoEntityCellCountDto>> GetTopGeoEntitiesByCellCountAsync<TEntity>(
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null)
        where TEntity : BaseEntity, new()
    {
        var cellsQuery = _dbContext.Cells
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Location != null);

        if (startDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt <= endDate.Value);

        var results = await _dbContext.Set<TEntity>()
            .AsNoTracking()
            .Where(geo => EF.Property<MultiPolygon>(geo, "Boundaries") != null)
            .Select(geo => new GeoEntityCellCountDto
            {
                Id = geo.Id,
                Name = EF.Property<string>(geo, "Name"),
                Code = EF.Property<string>(geo, "Code"),
                EntityType = typeof(TEntity).Name,
                CellCount = cellsQuery.Count(c =>
                    EF.Property<MultiPolygon>(geo, "Boundaries")!.Intersects(c.Location))
            })
            .Where(dto => dto.CellCount > 0) // Filter out entities with no cells
            .OrderByDescending(dto => dto.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .ToListAsync();

        return results;
    }

    /// <summary>
    /// Gets top child entities by cell count with optimized spatial queries.
    /// OPTIMIZED: Uses single batched query with GroupJoin to minimize database roundtrips.
    /// </summary>
    public async Task<List<GeoEntityCellCountDto>> GetTopChildEntitiesByCellCountAsync<TParent, TChild>(
        Guid? parentId,
        string parentIdPropertyName,
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null)
        where TParent : BaseEntity
        where TChild : BaseEntity
    {
        // Build reusable cell query
        var cellsQuery = _dbContext.Cells
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Location != null);

        if (startDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt <= endDate.Value);

        // Build child entity query with optional parent filtering
        var childQuery = _dbContext.Set<TChild>()
            .AsNoTracking()
            .Where(child => EF.Property<MultiPolygon>(child, "Boundaries") != null);

        if (parentId.HasValue)
        {
            childQuery = childQuery.Where(child =>
                EF.Property<Guid>(child, parentIdPropertyName) == parentId.Value);
        }

        var results = await childQuery
            .Select(child => new GeoEntityCellCountDto
            {
                Id = child.Id,
                Name = EF.Property<string>(child, "Name"),
                Code = EF.Property<string>(child, "Code"),
                EntityType = typeof(TChild).Name,
                CellCount = cellsQuery.Count(c =>
                    EF.Property<MultiPolygon>(child, "Boundaries")!.Intersects(c.Location))
            })
            .Where(dto => dto.CellCount > 0) // Filter out entities with no cells
            .OrderByDescending(dto => dto.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .ToListAsync();

        return results;
    }

    /// <summary>
    /// Gets cell sites grouped by service provider with optimized queries.
    /// OPTIMIZED: Single query with proper filtering and spatial index usage.
    /// </summary>
    public async Task<CellSitesByProviderResponseDto> GetCellSitesByProviderAsync(
        Guid? regionId = null,
        Guid? serviceProviderId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (serviceProviderId.HasValue)
        {
            var providerInfo = await _dbContext.ServiceProviders
                .AsNoTracking()
                .Where(sp => sp.Id == serviceProviderId.Value && !sp.IsDeleted)
                .Select(sp => new { Exists = true, sp.Service.Type })
                .FirstOrDefaultAsync(cancellationToken);

            if (providerInfo == null)
            {
                throw new ArgumentException($"Service Provider with ID {serviceProviderId} not found");
            }

            if (providerInfo.Type != ServiceTypeEnum.Wireless)
            {
                throw new ArgumentException($"Service Provider with ID {serviceProviderId} is not a wireless service provider. Only wireless providers are supported for cell site statistics.");
            }
        }

        MultiPolygon? regionBoundary = null;
        if (regionId.HasValue)
        {
            regionBoundary = await _dbContext.Regions
                .AsNoTracking()
                .Where(r => r.Id == regionId.Value && !r.IsDeleted)
                .Select(r => r.Boundaries)
                .FirstOrDefaultAsync(cancellationToken);

            if (regionBoundary == null)
            {
                throw new ArgumentException($"Region with ID {regionId} not found");
            }
        }

        var cellQuery = _dbContext.Cells
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Location != null && c.ServiceProvider.Service.Type == ServiceTypeEnum.Wireless);

        if (serviceProviderId.HasValue)
            cellQuery = cellQuery.Where(c => c.ServiceProviderId == serviceProviderId.Value);

        if (startDate.HasValue)
            cellQuery = cellQuery.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            cellQuery = cellQuery.Where(c => c.CreatedAt <= endDate.Value);

        if (regionBoundary != null)
        {
            cellQuery = cellQuery.Where(c => regionBoundary.Intersects(c.Location));
        }

        var providerCellCounts = await cellQuery
            .GroupBy(c => new {
                c.ServiceProviderId,
                ProviderName = c.ServiceProvider.Provider.Name,
                ProviderColor = c.ServiceProvider.Provider.Color
            })
            .Select(g => new CellSitesByProviderDto
            {
                ServiceProviderId = g.Key.ServiceProviderId,
                ProviderName = g.Key.ProviderName,
                ProviderColor = g.Key.ProviderColor,
                CellCount = g.Count(),
                Percentage = 0 // Calculated in-memory
            })
            .OrderByDescending(p => p.CellCount)
            .ToListAsync(cancellationToken);

        // Calculate percentages in-memory (faster than database calculation)
        var totalCellSites = providerCellCounts.Sum(p => p.CellCount);
        if (totalCellSites > 0)
        {
            foreach (var provider in providerCellCounts)
            {
                provider.Percentage = Math.Round((double)provider.CellCount / totalCellSites * 100, 2);
            }
        }

        return new CellSitesByProviderResponseDto
        {
            TotalCellSites = totalCellSites,
            Providers = providerCellCounts
        };
    }
}
