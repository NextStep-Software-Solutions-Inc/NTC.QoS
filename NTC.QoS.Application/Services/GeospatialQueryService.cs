using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;
using NTC.QoS.Infrastructure.Persistence;
using NetTopologySuite.Geometries;

namespace NTC.QoS.Application.Services;

public class GeospatialQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public GeospatialQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<GeoEntityCellCountDto>> GetTopGeoEntitiesByCellCountAsync<TEntity>(int topCount)
        where TEntity : BaseEntity, new()
    {
        var query = _dbContext.Set<TEntity>()
            .Select(geoEntity => new
            {
                Id = geoEntity.Id,
                Name = EF.Property<string>(geoEntity, "Name"),
                Code = EF.Property<string>(geoEntity, "Code"),
                BoundariesGeometry = EF.Property<MultiPolygon>(geoEntity, "BoundariesGeometry"),
                EntityType = typeof(TEntity).Name
            });

        var results = await query
            .Select(geo => new GeoEntityCellCountDto
            {
                Id = geo.Id,
                Name = geo.Name,
                Code = geo.Code,
                EntityType = geo.EntityType,
                CellCount = _dbContext.Cells.Count(c =>
                    EF.Property<Point>(c, "LocationGeometry") != null &&
                    geo.BoundariesGeometry != null &&
                    EF.Property<Point>(c, "LocationGeometry").Within(geo.BoundariesGeometry)
                )
            })
            .OrderByDescending(dto => dto.CellCount)
            .Take(topCount > 0 ? topCount : int.MaxValue)
            .ToListAsync();

        return results;
    }

    public async Task<List<GeoEntityCellCountDto>> GetTopChildEntitiesByCellCountAsync<TParent, TChild>(
        Guid parentId,
        string parentIdPropertyName,
        int topCount)
        where TParent : BaseEntity
        where TChild : BaseEntity
    {
        // Check if parent exists
        var parent = await _dbContext.Set<TParent>().FindAsync(parentId);
        if (parent == null)
            return new List<GeoEntityCellCountDto>();

        var query = _dbContext.Set<TChild>()
            .Where(child =>
                EF.Property<Guid>(child, parentIdPropertyName) == parentId
            )
            .Select(child => new
            {
                child.Id,
                Name = EF.Property<string>(child, "Name"),
                Code = EF.Property<string>(child, "Code"),
                BoundariesGeometry = EF.Property<MultiPolygon>(child, "BoundariesGeometry"),
                EntityType = typeof(TChild).Name
            });

        var results = await query
            .Select(dto => new GeoEntityCellCountDto
            {
                Id = dto.Id,
                Name = dto.Name,
                Code = dto.Code,
                EntityType = dto.EntityType,
                CellCount = _dbContext.Cells.Count(c =>
                    EF.Property<Point>(c, "LocationGeometry") != null &&
                    dto.BoundariesGeometry != null &&
                    EF.Property<Point>(c, "LocationGeometry").Within(dto.BoundariesGeometry)
                )
            })
            .OrderByDescending(x => x.CellCount)
            .Take(topCount > 0 ? topCount : int.MaxValue)
            .ToListAsync();

        return results;
    }

    public async Task<List<GeoEntityCellCountDto>> GetTopProvincesInRegionByCellCountAsync(Guid regionId, int topCount)
    {
        var region = await _dbContext.Regions.FindAsync(regionId);
        if (region == null)
            return new List<GeoEntityCellCountDto>();

        var results = await _dbContext.Provinces
            .Where(p => p.RegionId == regionId)
            .Select(province => new GeoEntityCellCountDto
            {
                Id = province.Id,
                Name = province.Name,
                Code = province.Code,
                EntityType = nameof(Province),
                CellCount = _dbContext.Cells.Count(c =>
                    EF.Property<Point>(c, "LocationGeometry") != null &&
                    province.BoundariesGeometry != null &&
                    EF.Property<Point>(c, "LocationGeometry").Within(province.BoundariesGeometry)
                )
            })
            .OrderByDescending(dto => dto.CellCount)
            .Take(topCount > 0 ? topCount : int.MaxValue)
            .ToListAsync();

        return results;
    }
}
