using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Services.Interface;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Services;

public class GeographicFilteringService : IGeographicFilteringService
{
    private readonly IRepository<Region> _regionRepository;
    private readonly ILogger<GeographicFilteringService> _logger;

    // Define the regions that belong to each major area
    private static readonly Dictionary<string, string[]> AreaRegions = new()
    {
        ["luzon"] =
        [
            "Region I (Ilocos Region)", "Region II (Cagayan Valley)", "Region III (Central Luzon)",
            "Region IV-A (Calabarzon)", "Mimaropa Region", "Region V (Bicol Region)",
            "National Capital Region (NCR)", "Cordillera Administrative Region (CAR)"
        ],
        ["visayas"] =
        [
            "Region VI (Western Visayas)", "Region VII (Central Visayas)", "Region VIII (Eastern Visayas)"
        ],
        ["mindanao"] =
        [
            "Region IX (Zamboanga Peninsula)", "Region X (Northern Mindanao)", "Region XI (Davao Region)",
            "Region XII (Soccsksargen)", "Region XIII (Caraga)",
            "Bangsamoro Autonomous Region In Muslim Mindanao (BARMM)"
        ]
    };

    public GeographicFilteringService(
        IRepository<Region> regionRepository,
        ILogger<GeographicFilteringService> logger)
    {
        _regionRepository = regionRepository;
        _logger = logger;
    }

    public async Task<IQueryable<ServiceProvider>> ApplyGeographicFiltersAsync(
        IQueryable<ServiceProvider> query,
        List<string>? areas,
        List<string>? regions,
        CancellationToken cancellationToken)
    {
        var targetRegions = GetTargetRegions(areas, regions);

        if (targetRegions.Count == 0)
            return query;

        _logger.LogInformation("Applying geographic filter for {RegionCount} regions: {Regions}",
            targetRegions.Count, string.Join(", ", targetRegions));

        try
        {
            // Always use the individual region approach for better reliability
            return await ApplyIndividualRegionFilterAsync(query, targetRegions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Geographic filtering failed for regions: {Regions}. Falling back to bounding box approach.",
                string.Join(", ", targetRegions));

            // Fallback to bounding box approach
            return await ApplyBoundingBoxFallbackAsync(query, targetRegions, cancellationToken);
        }
    }

    private async Task<IQueryable<ServiceProvider>> ApplyIndividualRegionFilterAsync(
        IQueryable<ServiceProvider> query,
        HashSet<string> targetRegions,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get region names that exist in database
            var existingRegionNames = await _regionRepository
                .Query()
                .Where(r => targetRegions.Contains(r.Name) && r.Boundaries != null)
                .Select(r => r.Name)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} regions out of {Requested} requested regions: {Regions}",
                existingRegionNames.Count, targetRegions.Count, string.Join(", ", existingRegionNames));

            if (existingRegionNames.Count == 0)
            {
                _logger.LogWarning("No region boundaries found for requested regions: {Regions}",
                    string.Join(", ", targetRegions));
                return query.Where(sp => false);
            }

            // Log missing regions
            var missingRegions = targetRegions.Except(existingRegionNames).ToList();
            if (missingRegions.Count > 0)
            {
                _logger.LogWarning("Missing region boundaries for: {MissingRegions}", string.Join(", ", missingRegions));
            }

            // Since EF Core has issues with complex spatial queries, go straight to client-side evaluation
            _logger.LogInformation("Using client-side evaluation for spatial filtering due to EF Core limitations");
            return await ApplyClientSideFilteringAsync(query, targetRegions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Individual region filtering failed, trying client-side evaluation");
            return await ApplyClientSideFilteringAsync(query, targetRegions, cancellationToken);
        }
    }

    private async Task<IQueryable<ServiceProvider>> ApplyClientSideFilteringAsync(
        IQueryable<ServiceProvider> query,
        HashSet<string> targetRegions,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Using client-side evaluation approach for spatial filtering");

            // Get region boundaries in memory
            var regionBoundaries = await _regionRepository
                .Query(r => targetRegions.Contains(r.Name) && r.Boundaries != null)
                .Select(r => new { r.Name, r.Boundaries })
                .ToListAsync(cancellationToken);

            if (regionBoundaries.Count == 0)
            {
                return query.Where(sp => false);
            }

            // Get all service providers with their locations and evaluate in memory
            var serviceProvidersWithLocations = await query
                .Select(sp => new {
                    ServiceProvider = sp,
                    ServiceType = sp.Service.Type,
                    CellLocations = sp.Cells.Where(c => c.Location != null).Select(c => c.Location),
                    NAPLocations = sp.NAPs.Where(n => n.Location != null).Select(n => n.Location)
                })
                .ToListAsync(cancellationToken);

            // Filter in memory
            var matchingIds = new List<Guid>();
            foreach (var item in serviceProvidersWithLocations)
            {
                bool hasMatch = false;
                IEnumerable<Point?> locations;

                // Determine which locations to check based on service type
                if (item.ServiceType == ServiceTypeEnum.Wireless)
                {
                    locations = item.CellLocations;
                }
                else if (item.ServiceType == ServiceTypeEnum.Wired)
                {
                    locations = item.NAPLocations;
                }
                else
                {
                    continue; // Skip unknown service types
                }

                foreach (var location in locations)
                {
                    if (location == null) continue;

                    foreach (var regionBoundary in regionBoundaries)
                    {
                        try
                        {
                            if (regionBoundary.Boundaries!.Intersects(location))
                            {
                                hasMatch = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Spatial intersection failed for region {Region}", regionBoundary.Name);
                            continue;
                        }
                    }
                    if (hasMatch) break;
                }

                if (hasMatch)
                {
                    matchingIds.Add(item.ServiceProvider.Id);
                }
            }

            _logger.LogInformation("Client-side evaluation found {Count} matching service providers", matchingIds.Count);

            // Return filtered query based on matching IDs
            return query.Where(sp => matchingIds.Contains(sp.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client-side filtering failed");
            throw;
        }
    }

    private async Task<IQueryable<ServiceProvider>> ApplyBoundingBoxFallbackAsync(
        IQueryable<ServiceProvider> query,
        HashSet<string> targetRegions,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Using bounding box fallback approach for {RegionCount} regions", targetRegions.Count);

            var regionEnvelopes = await _regionRepository
                .Query(r => targetRegions.Contains(r.Name) && r.Boundaries != null)
                .Select(r => new {
                    r.Name,
                    Envelope = r.Boundaries!.EnvelopeInternal,
                    r.Boundaries.SRID
                })
                .ToListAsync(cancellationToken);

            if (regionEnvelopes.Count == 0)
            {
                _logger.LogError("No region envelopes found for bounding box fallback");
                return query.Where(sp => false);
            }

            var factory = NtsGeometryServices.Instance.CreateGeometryFactory(regionEnvelopes.First().SRID);
            var boundingBoxes = new List<Geometry>();

            foreach (var envelope in regionEnvelopes)
            {
                try
                {
                    var bbox = factory.ToGeometry(envelope.Envelope);
                    boundingBoxes.Add(bbox);
                    _logger.LogDebug("Created bounding box for region: {RegionName}", envelope.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create bounding box for region: {RegionName}", envelope.Name);
                }
            }

            if (boundingBoxes.Count == 0)
            {
                _logger.LogError("No valid bounding boxes created");
                return query.Where(sp => false);
            }

            return query.Where(sp =>
                boundingBoxes.Any(bbox =>
                    sp.Service.Type == ServiceTypeEnum.Wireless &&
                     sp.Cells.Any(c => c.Location != null && bbox.Intersects(c.Location)) ||
                    sp.Service.Type == ServiceTypeEnum.Wired &&
                     sp.NAPs.Any(n => n.Location != null && bbox.Intersects(n.Location))
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bounding box fallback also failed");
            throw new InvalidOperationException($"All geographic filtering approaches failed for regions: {string.Join(", ", targetRegions)}", ex);
        }
    }

    private static HashSet<string> GetTargetRegions(List<string>? areas, List<string>? regions)
    {
        var targetRegions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add regions from Areas mapping
        if (areas?.Count > 0)
        {
            foreach (var area in areas)
            {
                var areaKey = area.Trim().ToLowerInvariant();
                if (AreaRegions.TryGetValue(areaKey, out var mappedRegions))
                {
                    foreach (var region in mappedRegions)
                    {
                        targetRegions.Add(region);
                    }
                }
            }
        }

        // Add direct regions
        if (regions?.Count > 0)
        {
            foreach (var region in regions)
            {
                targetRegions.Add(region.Trim());
            }
        }

        return targetRegions;
    }
}