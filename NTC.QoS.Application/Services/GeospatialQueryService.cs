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
        
        // Get all regions or specific region first (lightweight query)
        var regionsQuery = _dbContext.Regions.AsQueryable();
        if (regionId.HasValue)
        {
            regionsQuery = regionsQuery.Where(r => r.Id == regionId.Value);
        }
        var regions = await regionsQuery
            .Where(r => !r.IsDeleted)
            .Select(r => new { r.Id, r.Name, r.Code, r.Boundaries })
            .ToListAsync();

        // Early return if no regions found
        if (!regions.Any())
        {
            return new WirelessCoverageResponseDto 
            { 
                OverallCoverage = new List<WirelessCoverageDto>(),
                RegionsCoverage = new List<RegionWirelessCoverageDto>()
            };
        }

        // Get technologies with all their bands consolidated (more efficient)
        var technologies = await _dbContext.Technologies
            .Include(t => t.Generation)
            .Include(t => t.TechnologyBands)
                .ThenInclude(tb => tb.Band)
            .Where(t => !t.IsDeleted)
            .ToDictionaryAsync(t => t.Id, t => new
            {
                t.Id,
                TechnologyName = t.Generation.Name,
                TechnologyStandard = t.Standard,
                TechnologyBandIds = t.TechnologyBands.Where(tb => !tb.IsDeleted && !tb.Band.IsDeleted).Select(tb => tb.Id).ToList()
            });

        // Get all networks with their technology band information (single query)
        var networksWithTechBands = await _dbContext.Networks
            .Include(n => n.UpLink)
            .Include(n => n.DownLink)
            .Where(n => !n.IsDeleted)
            .ToDictionaryAsync(n => n.Id, n => new
            {
                n.Id,
                UpLinkTechBandId = n.UpLink.TechnologyBandId,
                DownLinkTechBandId = n.DownLink.TechnologyBandId
            });

        // Pre-calculate coverage data with a single optimized query
        var regionIds = regions.Select(r => r.Id).ToList();
        var networkIds = networksWithTechBands.Keys.ToList();
        
        var coverageData = await _dbContext.CellularCoverages
            .Where(cc => networkIds.Contains(cc.NetworkId) &&
                        cc.Strength >= MinimumSignalStrength &&
                        (startDate == null || cc.CreatedAt >= startDate) &&
                        (endDate == null || cc.CreatedAt <= endDate))
            .Join(_dbContext.Regions.Where(r => regionIds.Contains(r.Id)),
                  cc => true,
                  r => true,
                  (cc, r) => new { cc, r })
            .Where(x => x.r.Boundaries != null && 
                       x.cc.Location != null &&
                       x.cc.Location.IsWithinDistance(x.r.Boundaries, 0))
            .Select(x => new
            {
                x.cc.NetworkId,
                RegionId = x.r.Id,
                x.cc.Strength
            })
            .ToListAsync();

        var result = new WirelessCoverageResponseDto();

        // Build lookup tables for efficient processing - map networks to technologies
        var networkToTechnologies = new Dictionary<Guid, List<Guid>>();
        foreach (var network in networksWithTechBands)
        {
            var networkTechnologies = new HashSet<Guid>();
            
            // Find technologies that contain the network's uplink and downlink technology bands
            foreach (var technology in technologies.Values)
            {
                if (technology.TechnologyBandIds.Contains(network.Value.UpLinkTechBandId) ||
                    technology.TechnologyBandIds.Contains(network.Value.DownLinkTechBandId))
                {
                    networkTechnologies.Add(technology.Id);
                }
            }
            
            networkToTechnologies[network.Key] = networkTechnologies.ToList();
        }

        // Group coverage data for efficient lookups using ToDictionary
        var coverageByRegionAndNetwork = coverageData
            .GroupBy(cd => new { cd.RegionId, cd.NetworkId })
            .ToDictionary(g => g.Key, g => g.ToList());

        // Calculate overall coverage for each technology (consolidated by technology, not band)
        var overallCoverage = new List<WirelessCoverageDto>();
        
        foreach (var technology in technologies)
        {
            var regionsWithCoverage = new HashSet<Guid>();
            
            // Find all networks that use this technology (any of its bands)
            var networksForThisTechnology = networkToTechnologies
                .Where(kvp => kvp.Value.Contains(technology.Key))
                .Select(kvp => kvp.Key)
                .ToList();
            
            // Check which regions have coverage for this technology
            foreach (var region in regions)
            {
                var hasRegionCoverage = networksForThisTechnology.Any(networkId =>
                    coverageByRegionAndNetwork.ContainsKey(new { RegionId = region.Id, NetworkId = networkId }));
                
                if (hasRegionCoverage)
                    regionsWithCoverage.Add(region.Id);
            }

            var coveragePercentage = regions.Count > 0 ? (decimal)regionsWithCoverage.Count / regions.Count * 100 : 0;
            
            overallCoverage.Add(new WirelessCoverageDto
            {
                Technology = $"{technology.Value.TechnologyName} - {technology.Value.TechnologyStandard}",
                CoveragePercentage = Math.Round(coveragePercentage, 2),
                CoverageCount = regionsWithCoverage.Count,
                TotalRegionCount = regions.Count
            });
        }

        result.OverallCoverage = overallCoverage;

        // Only calculate detailed region coverage if a specific region is requested
        var regionsCoverage = new List<RegionWirelessCoverageDto>();
        
        if (regionId.HasValue)
        {
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
                    // Find networks that use this technology and have coverage in this region
                    var networksForThisTechnology = networkToTechnologies
                        .Where(kvp => kvp.Value.Contains(technology.Key))
                        .Select(kvp => kvp.Key)
                        .ToList();

                    // Get coverage points for this technology in this region
                    var coveragePointsForRegion = coverageData
                        .Where(cd => cd.RegionId == region.Id && 
                                    networksForThisTechnology.Contains(cd.NetworkId))
                        .ToList();

                    var hasCoverage = coveragePointsForRegion.Any();
                    var averageSignalStrength = hasCoverage ? coveragePointsForRegion.Average(cp => cp.Strength) : 0;
                    
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
                        Technology = $"{technology.Value.TechnologyName} - {technology.Value.TechnologyStandard}",
                        CoveragePercentage = Math.Round(coveragePercentage, 2),
                        AverageSignalStrength = Math.Round(averageSignalStrength, 2),
                        CoveragePointsCount = coveragePointsForRegion.Count
                    });
                }

                regionCoverage.OverallCoveragePercentage = technologies.Count > 0 
                    ? Math.Round((decimal)coveredTechnologiesCount / technologies.Count * 100, 2) 
                    : 0;

                regionsCoverage.Add(regionCoverage);
            }
        }

        result.RegionsCoverage = regionsCoverage;

        return result;
    }
}