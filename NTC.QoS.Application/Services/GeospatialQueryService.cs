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

    public async Task<List<GeoEntityCellCountDto>> GetTopGeoEntitiesByCellCountAsync<TEntity>(
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null)
        where TEntity : BaseEntity, new()
    {
        var geoEntitiesQuery = _dbContext.Set<TEntity>()
            .Select(geoEntity => new
            {
                Id = geoEntity.Id,
                Name = EF.Property<string>(geoEntity, "Name"),
                Code = EF.Property<string>(geoEntity, "Code"),
                Boundaries = EF.Property<MultiPolygon>(geoEntity, "Boundaries"),
                EntityType = typeof(TEntity).Name
            });

        var cellsQuery = _dbContext.Cells
            .Where(c => !c.IsDeleted &&
                        EF.Property<Point>(c, "Location") != null &&
                        (startDate == null || c.CreatedAt >= startDate) &&
                        (endDate == null || c.CreatedAt <= endDate));

        var results = await (
            from geo in geoEntitiesQuery
            join cell in cellsQuery
                on 1 equals 1 into cellGroup
            from cell in cellGroup
                .Where(c => geo.Boundaries != null &&
                           EF.Property<Point>(c, "Location").Within(geo.Boundaries))
                .DefaultIfEmpty()
            group cell by new { geo.Id, geo.Name, geo.Code, geo.EntityType } into g
            select new GeoEntityCellCountDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Code = g.Key.Code,
                EntityType = g.Key.EntityType,
                CellCount = g.Count(c => c != null)
            })
            .OrderByDescending(dto => dto.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .ToListAsync();

        return results;
    }

    public async Task<List<GeoEntityCellCountDto>> GetTopChildEntitiesByCellCountAsync<TParent, TChild>(
        Guid? parentId,
        string parentIdPropertyName,
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null)
        where TParent : BaseEntity
        where TChild : BaseEntity
    {
        IQueryable<TChild> query = _dbContext.Set<TChild>();

        // If parentId is null, act like GetTopGeoEntitiesByCellCountAsync (no parent filtering)
        // Otherwise, filter by parentId
        if (parentId.HasValue)
        {
            // Optional: You might still want to check if the parent actually exists,
            // but for acting like topGeoEntities when null, this check isn't strictly necessary for the 'else' branch.
            // If parentId is provided but doesn't exist, the Where clause will simply yield no results.
            query = query.Where(child =>
                EF.Property<Guid>(child, parentIdPropertyName) == parentId.Value
            );
        }

        var projectedQuery = query
            .Select(child => new
            {
                child.Id,
                Name = EF.Property<string>(child, "Name"),
                Code = EF.Property<string>(child, "Code"),
                Boundaries = EF.Property<MultiPolygon>(child, "Boundaries"),
                EntityType = typeof(TChild).Name
            });

        var cellsQuery = _dbContext.Cells
            .Where(c => !c.IsDeleted &&
                        EF.Property<Point>(c, "Location") != null &&
                        (startDate == null || c.CreatedAt >= startDate) &&
                        (endDate == null || c.CreatedAt <= endDate));

        var results = await (
            from geo in projectedQuery
            join cell in cellsQuery
                on 1 equals 1 into cellGroup
            from cell in cellGroup
                .Where(c => geo.Boundaries != null &&
                           EF.Property<Point>(c, "Location").Within(geo.Boundaries))
                .DefaultIfEmpty()
            group cell by new { geo.Id, geo.Name, geo.Code, geo.EntityType } into g
            select new GeoEntityCellCountDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Code = g.Key.Code,
                EntityType = g.Key.EntityType,
                CellCount = g.Count(c => c != null)
            })
            .OrderByDescending(x => x.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .ToListAsync();

        return results;
    }

    public async Task<CellSitesByProviderResponseDto> GetCellSitesByProviderAsync(
        Guid? regionId = null,
        Guid? serviceProviderId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (serviceProviderId.HasValue)
        {
            var providerType = await _dbContext.ServiceProviders
                .Where(sp => sp.Id == serviceProviderId.Value && !sp.IsDeleted)
                .Select(sp => sp.Service.Type)
                .FirstOrDefaultAsync(cancellationToken);

            var providerExists = await _dbContext.ServiceProviders
                .AnyAsync(sp => sp.Id == serviceProviderId.Value && !sp.IsDeleted, cancellationToken);

            if (!providerExists)
            {
                throw new ArgumentException($"Service Provider with ID {serviceProviderId} not found");
            }

            if (providerType != ServiceTypeEnum.Wireless)
            {
                throw new ArgumentException($"Service Provider with ID {serviceProviderId} is not a wireless service provider. Only wireless providers are supported for cell site statistics.");
            }
        }

        MultiPolygon? regionBoundary = null;

        if (regionId.HasValue)
        {
            var region = await _dbContext.Regions
                .Where(r => r.Id == regionId.Value && !r.IsDeleted)
                .Select(r => new { r.Name, r.Boundaries })
                .FirstOrDefaultAsync(cancellationToken);

            if (region == null)
            {
                throw new ArgumentException($"Region with ID {regionId} not found");
            }

            regionBoundary = region.Boundaries;
        }

        var cellQuery = _dbContext.Cells
            .Where(c => !c.IsDeleted && c.Location != null && c.ServiceProvider.Service.Type == ServiceTypeEnum.Wireless);

        if (serviceProviderId.HasValue)
        {
            cellQuery = cellQuery.Where(c => c.ServiceProviderId == serviceProviderId.Value);
        }

        if (startDate.HasValue)
        {
            cellQuery = cellQuery.Where(c => c.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            cellQuery = cellQuery.Where(c => c.CreatedAt <= endDate.Value);
        }

        if (regionBoundary != null)
        {
            cellQuery = cellQuery.Where(c => regionBoundary.Intersects(c.Location));
        }

        var providerCellCounts = await cellQuery
            .GroupBy(c => new {
                ServiceProviderId = c.ServiceProviderId,
                ProviderName = c.ServiceProvider.Provider.Name,
                ProviderColor = c.ServiceProvider.Provider.Color
            })
            .Select(g => new CellSitesByProviderDto
            {
                ServiceProviderId = g.Key.ServiceProviderId,
                ProviderName = g.Key.ProviderName,
                ProviderColor = g.Key.ProviderColor,
                CellCount = g.Count(),
                Percentage = 0
            })
            .OrderByDescending(p => p.CellCount)
            .ToListAsync(cancellationToken);

        var totalCellSites = providerCellCounts.Sum(p => p.CellCount);

        foreach (var provider in providerCellCounts)
        {
            provider.Percentage = totalCellSites > 0
                ? Math.Round((double)provider.CellCount / totalCellSites * 100, 2)
                : 0;
        }

        return new CellSitesByProviderResponseDto
        {
            TotalCellSites = totalCellSites,
            Providers = providerCellCounts
        };
    }
}