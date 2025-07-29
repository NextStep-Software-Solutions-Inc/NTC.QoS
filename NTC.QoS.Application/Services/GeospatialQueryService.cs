using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;
using NTC.QoS.Domain.Models.DTOs.Dashboard;
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
        int limit,
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

        var results = await projectedQuery
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
            .Take(limit > 0 ? limit : int.MaxValue)
            .ToListAsync();

        return results;
    }

    public async Task<WirelessCoverageResponseDto> GetWirelessCoverageAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? regionId = null)
    {
        // Define minimum signal strength threshold for usable coverage (e.g., -100 dBm)
        const decimal MinimumSignalStrength = -100m;
        
        // Get all technologies with their generations and system types
        var technologiesQuery = _dbContext.Technologies
            .Include(t => t.Generation)
            .Include(t => t.SystemTypes)
                .ThenInclude(st => st.Networks)
                    .ThenInclude(n => n.CellularCoverages)
            .Where(t => !t.IsDeleted);

        var technologies = await technologiesQuery.ToListAsync();

        // Get all regions or specific region
        var regionsQuery = _dbContext.Regions.AsQueryable();
        if (regionId.HasValue)
        {
            regionsQuery = regionsQuery.Where(r => r.Id == regionId.Value);
        }
        var regions = await regionsQuery.Where(r => !r.IsDeleted).ToListAsync();

        var result = new WirelessCoverageResponseDto();

        // Calculate overall coverage for each technology
        var overallCoverage = new List<WirelessCoverageDto>();
        
        foreach (var technology in technologies)
        {
            var coverageCount = 0;
            
            // Count regions that have adequate cellular coverage for this technology
            foreach (var region in regions)
            {
                // Check if any networks of this technology have adequate coverage in this region
                var hasCoverage = false;
                
                foreach (var systemType in technology.SystemTypes)
                {
                    foreach (var network in systemType.Networks)
                    {
                        var networkHasAdequateCoverage = await _dbContext.CellularCoverages
                            .Where(cc => cc.NetworkId == network.Id &&
                                        cc.Strength >= MinimumSignalStrength && // Use signal strength threshold
                                        region.Boundaries != null &&
                                        cc.Location != null &&
                                        cc.Location.IsWithinDistance(region.Boundaries, 0) &&
                                        (startDate == null || cc.CreatedAt >= startDate) &&
                                        (endDate == null || cc.CreatedAt <= endDate))
                            .AnyAsync();
                        
                        if (networkHasAdequateCoverage)
                        {
                            hasCoverage = true;
                            break;
                        }
                    }
                    if (hasCoverage) break;
                }
                
                if (hasCoverage)
                    coverageCount++;
            }

            var coveragePercentage = regions.Count > 0 ? (decimal)coverageCount / regions.Count * 100 : 0;
            
            overallCoverage.Add(new WirelessCoverageDto
            {
                Technology = $"{technology.Generation.Name} - {technology.Standard}",
                CoveragePercentage = Math.Round(coveragePercentage, 2),
                CoverageCount = coverageCount,
                TotalRegionCount = regions.Count
            });
        }

        result.OverallCoverage = overallCoverage;

        // Calculate coverage per region
        var regionsCoverage = new List<RegionWirelessCoverageDto>();
        
        foreach (var region in regions)
        {
            var regionCoverage = new RegionWirelessCoverageDto
            {
                RegionId = region.Id,
                RegionName = region.Name,
                RegionCode = region.Code,
                TechnologiesCoverage = new List<RegionTechnologyCoverageDto>()
            };

            var coveredTechnologiesCount = 0;

            foreach (var technology in technologies)
            {
                // Get coverage data for this technology in this region
                var coveragePoints = new List<Domain.Entities.CellularCoverage>();
                
                foreach (var systemType in technology.SystemTypes)
                {
                    foreach (var network in systemType.Networks)
                    {
                        var networkCoveragePoints = await _dbContext.CellularCoverages
                            .Where(cc => cc.NetworkId == network.Id &&
                                        cc.Strength >= MinimumSignalStrength &&
                                        region.Boundaries != null &&
                                        cc.Location != null &&
                                        cc.Location.IsWithinDistance(region.Boundaries, 0) &&
                                        (startDate == null || cc.CreatedAt >= startDate) &&
                                        (endDate == null || cc.CreatedAt <= endDate))
                            .ToListAsync();
                        
                        coveragePoints.AddRange(networkCoveragePoints);
                    }
                }

                var hasCoverage = coveragePoints.Any();
                var averageSignalStrength = hasCoverage ? coveragePoints.Average(cp => cp.Strength) : 0;
                
                // Calculate coverage percentage based on signal strength quality
                var coveragePercentage = 0m;
                if (hasCoverage)
                {
                    // Simple quality-based percentage: stronger signals = higher percentage
                    var normalizedStrength = Math.Max(0, (averageSignalStrength - MinimumSignalStrength) / (-50m - MinimumSignalStrength));
                    coveragePercentage = Math.Min(100, Math.Max(10, normalizedStrength * 100)); // 10-100% range
                }

                if (hasCoverage)
                    coveredTechnologiesCount++;

                regionCoverage.TechnologiesCoverage.Add(new RegionTechnologyCoverageDto
                {
                    Technology = $"{technology.Generation.Name} - {technology.Standard}",
                    CoveragePercentage = Math.Round(coveragePercentage, 2),
                    AverageSignalStrength = Math.Round(averageSignalStrength, 2),
                    CoveragePointsCount = coveragePoints.Count
                });
            }

            regionCoverage.OverallCoveragePercentage = technologies.Count > 0 
                ? Math.Round((decimal)coveredTechnologiesCount / technologies.Count * 100, 2) 
                : 0;

            regionsCoverage.Add(regionCoverage);
        }

        result.RegionsCoverage = regionsCoverage;

        return result;
    }
}