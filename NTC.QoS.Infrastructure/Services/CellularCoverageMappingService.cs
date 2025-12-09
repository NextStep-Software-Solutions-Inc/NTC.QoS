using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Implementation of CellularCoverage mapping service for V1 enhancement.
/// Maps coverage points to infrastructure (CellSector and Municipality).
/// Optimized with in-memory caching for bulk processing.
/// </summary>
public class CellularCoverageMappingService : ICellularCoverageMappingService
{
    private readonly IRepository<CellSector> _cellSectorRepository;
    private readonly IRepository<Municipality> _municipalityRepository;
    private readonly ILogger<CellularCoverageMappingService> _logger;

    // In-memory cache for CellSectors
    private List<CellSectorCache>? _cellSectorCache;
    
    // In-memory cache for Municipalities (with spatial boundaries)
    private List<MunicipalityCache>? _municipalityCache;

    private class CellSectorCache
    {
        public Guid Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    private class MunicipalityCache
    {
        public Guid Id { get; set; }
        public MultiPolygon? Boundaries { get; set; }
        public double? CenterLatitude { get; set; }
        public double? CenterLongitude { get; set; }
    }

    public CellularCoverageMappingService(
        IRepository<CellSector> cellSectorRepository,
        IRepository<Municipality> municipalityRepository,
        ILogger<CellularCoverageMappingService> logger)
    {
        _cellSectorRepository = cellSectorRepository;
        _municipalityRepository = municipalityRepository;
        _logger = logger;
    }

    /// <summary>
    /// Pre-loads all CellSectors and Municipalities into memory for efficient lookups
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing CellularCoverage mapping service");

        // Load all CellSectors with their Cell locations
        _cellSectorCache = await _cellSectorRepository
            .Query(cs => !cs.IsDeleted)
            .Include(cs => cs.Cell)
            .Where(cs => cs.Cell != null && cs.Cell.Location != null && !cs.Cell.IsDeleted)
            .Select(cs => new CellSectorCache
            {
                Id = cs.Id,
                Latitude = cs.Cell!.Location.Y,
                Longitude = cs.Cell!.Location.X
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Loaded {Count} CellSectors into cache", _cellSectorCache.Count);

        // Load all Municipalities with their boundaries
        _municipalityCache = await _municipalityRepository
            .Query(m => !m.IsDeleted)
            .Select(m => new MunicipalityCache
            {
                Id = m.Id,
                Boundaries = m.Boundaries,
                CenterLatitude = m.Center != null ? m.Center.Y : (double?)null,
                CenterLongitude = m.Center != null ? m.Center.X : (double?)null
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Loaded {Count} Municipalities into cache", _municipalityCache.Count);
    }

    /// <summary>
    /// Finds the nearest CellSector and its Municipality for a single coverage point
    /// </summary>
    public Task<(Guid? cellSectorId, Guid? municipalityId)> FindInfrastructureMappingAsync(
        double latitude, 
        double longitude)
    {
        if (_cellSectorCache == null || _cellSectorCache.Count == 0)
        {
            _logger.LogWarning("CellSector cache not initialized or empty");
            return Task.FromResult<(Guid?, Guid?)>((null, null));
        }

        // Step 1: Find Municipality containing this point (spatial query)
        var municipalityId = FindMunicipalityContainingPoint(latitude, longitude);

        if (municipalityId == null)
        {
            _logger.LogDebug("No municipality found for point ({Lat}, {Lng})", latitude, longitude);
            return Task.FromResult<(Guid?, Guid?)>((null, null));
        }

        // Step 2: Find nearest CellSector within the municipality
        var cellSectorId = FindNearestCellSectorInMunicipality(latitude, longitude, municipalityId.Value);

        if (cellSectorId == null)
        {
            _logger.LogDebug("No CellSector found in municipality {MunicipalityId} for point ({Lat}, {Lng})", 
                municipalityId, latitude, longitude);
            return Task.FromResult<(Guid?, Guid?)>((null, municipalityId));
        }

        return Task.FromResult((cellSectorId, municipalityId));
    }

    /// <summary>
    /// Batch mapping for multiple coverage points (optimized)
    /// </summary>
    public Task<Dictionary<int, (Guid? cellSectorId, Guid? municipalityId)>> FindInfrastructureMappingBatchAsync(
        List<(double latitude, double longitude)> coveragePoints,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<int, (Guid?, Guid?)>();

        if (_cellSectorCache == null || _cellSectorCache.Count == 0)
        {
            _logger.LogWarning("CellSector cache not initialized or empty");
            return Task.FromResult(results);
        }

        for (int i = 0; i < coveragePoints.Count; i++)
        {
            var point = coveragePoints[i];
            
            // Find municipality
            var municipalityId = FindMunicipalityContainingPoint(point.latitude, point.longitude);
            
            if (municipalityId == null)
            {
                results[i] = (null, null);
                continue;
            }

            // Find nearest CellSector in municipality
            var cellSectorId = FindNearestCellSectorInMunicipality(
                point.latitude, point.longitude, municipalityId.Value);

            results[i] = (cellSectorId, municipalityId);
        }

        var mappedCount = results.Values.Count(r => r.Item1 != null);
        _logger.LogInformation("Batch mapping completed: {Total} points, {Mapped} mapped to CellSectors", 
            coveragePoints.Count, mappedCount);

        return Task.FromResult(results);
    }

    /// <summary>
    /// Finds which municipality contains the given point using spatial queries
    /// </summary>
    private Guid? FindMunicipalityContainingPoint(double latitude, double longitude)
    {
        if (_municipalityCache == null || _municipalityCache.Count == 0)
            return null;

        var point = GeometryHelper.CreatePoint(new Coordinate(x: longitude, y: latitude));

        // Check each municipality boundary
        foreach (var municipality in _municipalityCache)
        {
            if (municipality.Boundaries != null && municipality.Boundaries.Contains(point))
            {
                return municipality.Id;
            }
        }

        // Fallback: If no exact match, find nearest municipality by center
        var nearest = _municipalityCache
            .Where(m => m.CenterLatitude.HasValue && m.CenterLongitude.HasValue)
            .Select(m => new
            {
                m.Id,
                Distance = CalculateHaversineDistance(
                    latitude, longitude, 
                    m.CenterLatitude!.Value, m.CenterLongitude!.Value)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (nearest != null && nearest.Distance < 50000) // Within 50km
        {
            _logger.LogDebug("Point ({Lat}, {Lng}) not in any boundary, using nearest municipality {MunicipalityId} ({Distance}m away)",
                latitude, longitude, nearest.Id, nearest.Distance);
            return nearest.Id;
        }

        return null;
    }

    /// <summary>
    /// Finds the nearest CellSector within a specific municipality using in-memory cache
    /// </summary>
    private Guid? FindNearestCellSectorInMunicipality(double latitude, double longitude, Guid municipalityId)
    {
        if (_cellSectorCache == null || _cellSectorCache.Count == 0)
            return null;

        // For V1, we don't have municipality linkage in CellSector
        // So we'll find the nearest CellSector globally and verify it's reasonably close
        var nearest = _cellSectorCache
            .Select(cs => new
            {
                cs.Id,
                Distance = CalculateHaversineDistance(latitude, longitude, cs.Latitude, cs.Longitude)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        // Only return if within reasonable distance (e.g., 10km)
        if (nearest != null && nearest.Distance < 10000)
        {
            return nearest.Id;
        }

        _logger.LogDebug("No nearby CellSector found for point ({Lat}, {Lng}) in municipality {MunicipalityId}", 
            latitude, longitude, municipalityId);
        
        return null;
    }

    /// <summary>
    /// Calculate Haversine distance between two points (in meters)
    /// </summary>
    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c; // Distance in meters
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
