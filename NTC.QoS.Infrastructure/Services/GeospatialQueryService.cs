using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;
using NTC.QoS.Infrastructure.Persistence;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Interfaces; // Assuming this is where IGeospatialQueryService is

namespace NTC.QoS.Infrastructure.Services;

public class GeospatialQueryService : IGeospatialQueryService
{
    // REMOVE THIS LINE:
    // private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    // ADD THIS LINE: Inject ApplicationDbContext directly
    private readonly ApplicationDbContext _dbContext;

    // Modify the constructor to inject ApplicationDbContext
    public GeospatialQueryService(ApplicationDbContext dbContext) // Changed parameter
    {
        _dbContext = dbContext; // Assign the injected DbContext
    }

    public async Task<List<GeoEntityCellCountDto>> GetTopRegionsByCellCountAsync(
    int limit,
    DateTime? startDate = null,
    DateTime? endDate = null)
    {
        // Use the injected _dbContext directly, no need for CreateDbContextAsync()
        var query = _dbContext.Regions // Changed dbContext to _dbContext
            .Select(region => new
            {
                region.Id,
                region.Name,
                region.Code,
                region.Boundaries,
                EntityType = "Region"
            });

        var results = await query
            .Select(r => new GeoEntityCellCountDto
            {
                Id = r.Id,
                Name = r.Name,
                Code = r.Code,
                EntityType = r.EntityType,
                CellCount = _dbContext.Cells.Count(c => // Changed dbContext to _dbContext
                    c.Location != null &&
                    r.Boundaries != null &&
                    c.Location.IsWithinDistance(r.Boundaries, 0) &&
                    (startDate == null || c.CreatedAt >= startDate) &&
                    (endDate == null || c.CreatedAt <= endDate)
                )
            })
            .OrderByDescending(dto => dto.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .ToListAsync();

        return results;
    }

    public async Task<List<GeoEntityCellCountDto>> GetTopProvincesInRegionByCellCountAsync(
    Guid? regionId, int limit, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Use the injected _dbContext directly
        var query = _dbContext.Provinces.AsQueryable(); // Changed dbContext to _dbContext
        if (regionId.HasValue)
            query = query.Where(p => p.RegionId == regionId.Value);

        var projected = query.Select(province => new
        {
            province.Id,
            province.Name,
            province.Code,
            province.Boundaries,
            EntityType = "Province"
        });

        var results = await projected
            .Select(dto => new GeoEntityCellCountDto
            {
                Id = dto.Id,
                Name = dto.Name,
                Code = dto.Code,
                EntityType = dto.EntityType,
                CellCount = _dbContext.Cells.Count(c => // Changed dbContext to _dbContext
                    c.Location != null &&
                    dto.Boundaries != null &&
                    c.Location.IsWithinDistance(dto.Boundaries, 0) &&
                    (startDate == null || c.CreatedAt >= startDate) &&
                    (endDate == null || c.CreatedAt <= endDate)
                )
            })
            .OrderByDescending(x => x.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .ToListAsync();

        return results;
    }

    public async Task<List<GeoEntityCellCountDto>> GetTopMunicipalitiesInProvinceByCellCountAsync(
        Guid? provinceId, int limit, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Use the injected _dbContext directly
        var query = _dbContext.Municipalities.AsQueryable(); // Changed dbContext to _dbContext
        if (provinceId.HasValue)
            query = query.Where(m => m.ProvinceId == provinceId.Value);

        var projected = query.Select(municipality => new
        {
            municipality.Id,
            municipality.Name,
            municipality.Code,
            municipality.Boundaries,
            EntityType = "Municipality"
        });

        var results = await projected
            .Select(dto => new GeoEntityCellCountDto
            {
                Id = dto.Id,
                Name = dto.Name,
                Code = dto.Code,
                EntityType = dto.EntityType,
                CellCount = _dbContext.Cells.Count(c => // Changed dbContext to _dbContext
                    c.Location != null &&
                    dto.Boundaries != null &&
                    c.Location.IsWithinDistance(dto.Boundaries, 0) &&
                    (startDate == null || c.CreatedAt >= startDate) &&
                    (endDate == null || c.CreatedAt <= endDate)
                )
            })
            .OrderByDescending(x => x.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .ToListAsync();

        return results;
    }

    public async Task<List<GeoEntityCellCountDto>> GetTopBarangaysInMunicipalityByCellCountAsync(
        Guid? municipalityId, int limit, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Use the injected _dbContext directly
        var query = _dbContext.Barangays.AsQueryable(); // Changed dbContext to _dbContext
        if (municipalityId.HasValue)
            query = query.Where(b => b.MunicipalityId == municipalityId.Value);

        var projected = query.Select(barangay => new
        {
            barangay.Id,
            barangay.Name,
            barangay.Code,
            barangay.Boundaries,
            EntityType = "Barangay"
        });

        var results = await projected
            .Select(dto => new GeoEntityCellCountDto
            {
                Id = dto.Id,
                Name = dto.Name,
                Code = dto.Code,
                EntityType = dto.EntityType,
                CellCount = _dbContext.Cells.Count(c => // Changed dbContext to _dbContext
                    c.Location != null &&
                    dto.Boundaries != null &&
                    c.Location.IsWithinDistance(dto.Boundaries, 0) &&
                    (startDate == null || c.CreatedAt >= startDate) &&
                    (endDate == null || c.CreatedAt <= endDate)
                )
            })
            .OrderByDescending(x => x.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .ToListAsync();

        return results;
    }

    public async Task<List<GeoEntityCellCountDto>> GetTopGeoEntitiesByCellCountAsync<TEntity>(
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null)
        where TEntity : BaseEntity, new()
    {
        // Use the injected _dbContext directly
        var query = _dbContext.Set<TEntity>() // Changed dbContext to _dbContext
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
                CellCount = _dbContext.Cells.Count(c => // Changed dbContext to _dbContext
                    EF.Property<Point>(c, "Location") != null &&
                    geo.Boundaries != null &&
                    EF.Property<Point>(c, "Location").IsWithinDistance(geo.Boundaries, 0) &&
                    (startDate == null || c.CreatedAt >= startDate) &&
                    (endDate == null || c.CreatedAt <= endDate)
                )
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
        // Use the injected _dbContext directly
        IQueryable<TChild> query = _dbContext.Set<TChild>(); // Changed dbContext to _dbContext

        // If parentId is null, act like GetTopGeoEntitiesByCellCountAsync (no parent filtering)
        // Otherwise, filter by parentId
        if (parentId.HasValue)
        {
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

        var results = await projectedQuery
            .Select(dto => new GeoEntityCellCountDto
            {
                Id = dto.Id,
                Name = dto.Name,
                Code = dto.Code,
                EntityType = dto.EntityType,
                CellCount = _dbContext.Cells.Count(c => // Changed dbContext to _dbContext
                    EF.Property<Point>(c, "Location") != null &&
                    dto.Boundaries != null &&
                    EF.Property<Point>(c, "Location").IsWithinDistance(dto.Boundaries, 0) &&
                    (startDate == null || c.CreatedAt >= startDate) &&
                    (endDate == null || c.CreatedAt <= endDate)
                )
            })
            .OrderByDescending(x => x.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .ToListAsync();

        return results;
    }
}