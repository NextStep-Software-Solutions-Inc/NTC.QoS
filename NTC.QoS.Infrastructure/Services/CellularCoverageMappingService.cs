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
    
    // Lightweight cache of municipality IDs (boundaries queried from DB)
    private HashSet<Guid>? _municipalityIds;

    // Thread-safe initialization guard
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private class CellSectorCache
    {
        public Guid Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
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
    /// Pre-loads all CellSectors and Municipality IDs into memory for efficient lookups.
    /// Municipality boundaries are queried from database on-demand to avoid memory overhead.
    /// Thread-safe for concurrent initialization attempts.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Quick check without lock for already initialized state
        if (_cellSectorCache != null && _municipalityIds != null)
        {
            _logger.LogDebug("Mapping service already initialized, skipping reload.");
            return;
        }

        // Acquire lock for thread-safe initialization
        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock (another thread may have initialized)
            if (_cellSectorCache != null && _municipalityIds != null)
            {
                _logger.LogDebug("Mapping service already initialized by another thread, skipping reload.");
                return;
            }

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

            // Load only Municipality IDs (boundaries queried from DB on-demand)
            _municipalityIds = await _municipalityRepository
                .Query(m => !m.IsDeleted && m.Boundaries != null)
                .Select(m => m.Id)
                .ToHashSetAsync(cancellationToken);

            _logger.LogInformation("Loaded {Count} Municipality IDs into cache (boundaries queried from DB on-demand)", _municipalityIds.Count);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>
    /// Finds the nearest CellSector and its Municipality for a single coverage point
    /// </summary>
    public async Task<(Guid? cellSectorId, Guid? municipalityId)> FindInfrastructureMappingAsync(
        double latitude, 
        double longitude)
    {
        if (_municipalityIds == null || _municipalityIds.Count == 0)
        {
            _logger.LogWarning("Municipality cache not initialized or empty. Call InitializeAsync before using mapping service.");
            return (null, null);
        }

        // Step 1: Find Municipality containing this point (database spatial query)
        var municipalityId = await FindMunicipalityContainingPointAsync(latitude, longitude);

        if (municipalityId == null)
        {
            _logger.LogDebug("No municipality found for point ({Lat}, {Lng})", latitude, longitude);
            return (null, null);
        }

        // Step 2: Find nearest CellSector within the municipality
        var cellSectorId = await FindNearestCellSectorInMunicipalityAsync(latitude, longitude, municipalityId.Value);

        if (cellSectorId == null)
        {
            _logger.LogDebug("No CellSector found in municipality {MunicipalityId} for point ({Lat}, {Lng})", 
                municipalityId, latitude, longitude);
            return (null, municipalityId);
        }

        return (cellSectorId, municipalityId);
    }

    /// <summary>
    /// Batch mapping for multiple coverage points (optimized)
    /// </summary>
    public async Task<Dictionary<int, (Guid? cellSectorId, Guid? municipalityId)>> FindInfrastructureMappingBatchAsync(
        List<(double latitude, double longitude)> coveragePoints,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<int, (Guid?, Guid?)>();

        if (_municipalityIds == null || _municipalityIds.Count == 0)
        {
            _logger.LogWarning("Municipality cache not initialized or empty. Call InitializeAsync before using mapping service.");
            return results;
        }

        for (int i = 0; i < coveragePoints.Count; i++)
        {
            var point = coveragePoints[i];
            
            // Find municipality (database query)
            var municipalityId = await FindMunicipalityContainingPointAsync(point.latitude, point.longitude);
            
            if (municipalityId == null)
            {
                results[i] = (null, null);
                continue;
            }

            // Find nearest CellSector in municipality
            var cellSectorId = await FindNearestCellSectorInMunicipalityAsync(
                point.latitude, point.longitude, municipalityId.Value);

            results[i] = (cellSectorId, municipalityId);
        }

        var mappedCount = results.Values.Count(r => r.Item1 != null);
        _logger.LogInformation("Batch mapping completed: {Total} points, {Mapped} mapped to CellSectors", 
            coveragePoints.Count, mappedCount);

        return results;
    }

    /// <summary>
    /// Finds which municipality contains the given point using database spatial query.
    /// Uses PostGIS spatial index for optimal performance without loading boundaries into memory.
    /// </summary>
    private async Task<Guid?> FindMunicipalityContainingPointAsync(double latitude, double longitude)
    {
        var point = GeometryHelper.CreatePoint(new Coordinate(x: longitude, y: latitude));

        // Use database spatial query (PostGIS optimized with spatial index)
        // Pattern follows Repository.cs ApplyBarangayLocationFilters
        var municipalityId = await _municipalityRepository
            .Query(m => !m.IsDeleted && m.Boundaries != null && m.Boundaries.Contains(point))
            .Select(m => m.Id)
            .FirstOrDefaultAsync();

        return municipalityId;
    }

    /// <summary>
    /// Finds the nearest CellSector within a specific municipality.
    /// Uses in-memory Haversine for nearest calculation, then validates boundary in database.
    /// </summary>
    private async Task<Guid?> FindNearestCellSectorInMunicipalityAsync(double latitude, double longitude, Guid municipalityId)
    {
        if (_cellSectorCache == null || _cellSectorCache.Count == 0)
            return null;

        // Find nearest CellSector using in-memory Haversine distance
        var nearest = _cellSectorCache
            .Select(cs => new
            {
                cs.Id,
                cs.Latitude,
                cs.Longitude,
                Distance = CalculateHaversineDistance(latitude, longitude, cs.Latitude, cs.Longitude)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (nearest == null)
        {
            _logger.LogDebug("No CellSector found for point ({Lat}, {Lng})", latitude, longitude);
            return null;
        }

        // Validate CellSector is within the municipality boundary (database spatial query)
        var cellSectorPoint = GeometryHelper.CreatePoint(new Coordinate(x: nearest.Longitude, y: nearest.Latitude));
        var isWithinBoundary = await _municipalityRepository
            .Query(m => m.Id == municipalityId && m.Boundaries != null && m.Boundaries.Contains(cellSectorPoint))
            .AnyAsync();

        if (!isWithinBoundary)
        {
            _logger.LogDebug(
                "Nearest CellSector {CellSectorId} is outside municipality {MunicipalityId} boundary. Skipping to avoid cross-municipality association.",
                nearest.Id, municipalityId);
            return null;
        }

        return nearest.Id;
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
