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

    public async Task<List<GeoEntityCellCountDto>> GetTopGeoEntitiesByCellCountAsync<TEntity>(
        int topCount,
        DateTime? startDate = null,
        DateTime? endDate = null)
        where TEntity : BaseEntity, new()
    {
        var query = _dbContext.Set<TEntity>()
            .Select(geoEntity => new
            {
                Id = geoEntity.Id,
                Name = EF.Property<string>(geoEntity, "Name"),
                Code = EF.Property<string>(geoEntity, "Code"),
                Boundaries = EF.Property<MultiPolygon>(geoEntity, "Boundaries"),
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
                    EF.Property<Point>(c, "Location") != null &&
                    geo.Boundaries != null &&
                    EF.Property<Point>(c, "Location").IsWithinDistance(geo.Boundaries, 0) &&
                    (startDate == null || c.CreatedAt >= startDate) &&
                    (endDate == null || c.CreatedAt <= endDate)
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
        int topCount,
        DateTime? startDate = null,
        DateTime? endDate = null)
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
                Boundaries = EF.Property<MultiPolygon>(child, "Boundaries"),
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
                    EF.Property<Point>(c, "Location") != null &&
                    dto.Boundaries != null &&
                    EF.Property<Point>(c, "Location").IsWithinDistance(dto.Boundaries, 0) &&
                    (startDate == null || c.CreatedAt >= startDate) &&
                    (endDate == null || c.CreatedAt <= endDate)
                )
            })
            .OrderByDescending(x => x.CellCount)
            .Take(topCount > 0 ? topCount : int.MaxValue)
            .ToListAsync();

        return results;
    }
}