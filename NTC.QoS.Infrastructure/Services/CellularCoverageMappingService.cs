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
    /// Batch mapping for multiple coverage points with optimized spatial queries.
    /// OPTIMIZED FOR BULK PROCESSING:
    /// - Processes points in parallel batches (batch size: 20) to reduce sequential DB queries
    /// - Uses database-side spatial operations (Intersects) to avoid boundary materialization
    /// - Performs in-memory CellSector matching using cached Haversine calculations
    /// - Significantly reduces database roundtrips compared to sequential processing
    /// 
    /// Performance: For 1000 points, reduces from 1000 sequential queries to ~50 parallel batches.
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

        if (coveragePoints.Count == 0)
        {
            return results;
        }

        // Step 1: Bulk municipality lookup with optimized spatial query
        // OPTIMIZATION: Use database-side spatial operations instead of materializing boundaries
        var pointsWithIndices = coveragePoints
            .Select((p, idx) => new
            {
                Index = idx,
                Point = GeometryHelper.CreatePoint(new Coordinate(x: p.longitude, y: p.latitude)),
                Latitude = p.latitude,
                Longitude = p.longitude
            })
            .ToList();

        var pointMunicipalityMap = new Dictionary<int, Guid?>(); // index -> municipalityId

        // Process points in parallel batches to reduce sequential DB queries
        // Batch size tuned for optimal performance (trade-off between parallelism and DB load)
        const int batchSize = 20;
        var batches = pointsWithIndices
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            // Process batch of points in parallel
            var batchTasks = batch.Select(async item =>
            {
                var municipalityId = await _municipalityRepository
                    .Query(m => !m.IsDeleted && m.Boundaries != null && m.Boundaries.Intersects(item.Point))
                    .Select(m => m.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                return new { item.Index, MunicipalityId = municipalityId };
            });

            var batchResults = await Task.WhenAll(batchTasks);

            // Collect results from batch
            foreach (var result in batchResults)
            {
                pointMunicipalityMap[result.Index] = result.MunicipalityId;
            }
        }

        _logger.LogDebug("Bulk municipality lookup: {Mapped}/{Total} points mapped to municipalities",
            pointMunicipalityMap.Count(kvp => kvp.Value != null), coveragePoints.Count);

        // Step 2: For each unique municipality, fetch CellSectors once, then match all points
        // OPTIMIZATION: Group by municipality to minimize database queries
        var uniqueMunicipalityIds = pointMunicipalityMap.Values
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        // Pre-fetch CellSector IDs for each municipality (one query per municipality)
        var municipalityCellSectorCache = new Dictionary<Guid, HashSet<Guid>>();
        
        foreach (var municipalityId in uniqueMunicipalityIds)
        {
            var cellSectorIds = await _cellSectorRepository
                .Query(cs => !cs.IsDeleted && cs.Cell != null && cs.Cell.Location != null && !cs.Cell.IsDeleted)
                .Include(cs => cs.Cell)
                .Where(cs => _municipalityRepository
                    .Query(m => m.Id == municipalityId && m.Boundaries != null && m.Boundaries.Intersects(cs.Cell!.Location))
                    .Any())
                .Select(cs => cs.Id)
                .ToHashSetAsync(cancellationToken);

            municipalityCellSectorCache[municipalityId] = cellSectorIds;
        }

        _logger.LogDebug("Fetched CellSectors for {Count} unique municipalities", uniqueMunicipalityIds.Count);

        // Step 3: For each point, find nearest CellSector using pre-fetched data (pure in-memory)
        for (int i = 0; i < coveragePoints.Count; i++)
        {
            // Check if municipality was found
            if (!pointMunicipalityMap.TryGetValue(i, out var municipalityId) || municipalityId == null)
            {
                results[i] = (null, null);
                continue;
            }

            var point = coveragePoints[i];

            // Get pre-fetched CellSector IDs for this municipality
            if (!municipalityCellSectorCache.TryGetValue(municipalityId.Value, out var cellSectorIdsInMunicipality) 
                || cellSectorIdsInMunicipality.Count == 0)
            {
                results[i] = (null, municipalityId);
                continue;
            }

            // Find nearest CellSector from cached data (pure in-memory operation)
            var nearest = _cellSectorCache!
                .Where(cs => cellSectorIdsInMunicipality.Contains(cs.Id))
                .Select(cs => new
                {
                    cs.Id,
                    Distance = CalculateHaversineDistance(point.latitude, point.longitude, cs.Latitude, cs.Longitude)
                })
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            results[i] = (nearest?.Id, municipalityId);
        }

        var mappedCount = results.Values.Count(r => r.Item1 != null);
        _logger.LogInformation("Batch mapping completed: {Total} points, {Mapped} mapped to CellSectors, {Municipalities} unique municipalities", 
            coveragePoints.Count, mappedCount, uniqueMunicipalityIds.Count);

        return results;
    }

    /// <summary>
    /// Finds which municipality contains the given point using database spatial query.
    /// OPTIMIZED: Uses subquery approach with Intersects to avoid boundary materialization.
    /// This keeps spatial operations in the database and leverages PostGIS spatial indexes.
    /// </summary>
    private async Task<Guid?> FindMunicipalityContainingPointAsync(double latitude, double longitude)
    {
        var point = GeometryHelper.CreatePoint(new Coordinate(x: longitude, y: latitude));

        // Use subquery approach with Intersects (not Contains) to avoid boundary materialization
        // Follows pattern from GeoQueryHelper.ApplyBoundaryFilterAsync
        var municipalityId = await _municipalityRepository
            .Query(m => !m.IsDeleted && m.Boundaries != null && m.Boundaries.Intersects(point))
            .Select(m => m.Id)
            .FirstOrDefaultAsync();

        return municipalityId;
    }

    /// <summary>
    /// Finds the nearest CellSector within a specific municipality.
    /// OPTIMIZED: Filters CellSectors by municipality boundary FIRST using database spatial query,
    /// then calculates distances only for CellSectors within the municipality.
    /// This ensures accurate results and avoids returning null when valid CellSectors exist.
    /// </summary>
    private async Task<Guid?> FindNearestCellSectorInMunicipalityAsync(double latitude, double longitude, Guid municipalityId)
    {
        var coveragePoint = GeometryHelper.CreatePoint(new Coordinate(x: longitude, y: latitude));

        // CRITICAL FIX: Query database to find CellSectors within the municipality boundary FIRST
        // This avoids the logic bug where globally nearest CellSector outside municipality returns null
        var cellSectorsInMunicipality = await _cellSectorRepository
            .Query(cs => !cs.IsDeleted && cs.Cell != null && cs.Cell.Location != null && !cs.Cell.IsDeleted)
            .Include(cs => cs.Cell)
            .Where(cs => _municipalityRepository
                .Query(m => m.Id == municipalityId && m.Boundaries != null && m.Boundaries.Intersects(cs.Cell!.Location))
                .Any())
            .Select(cs => new
            {
                cs.Id,
                Latitude = cs.Cell!.Location.Y,
                Longitude = cs.Cell!.Location.X
            })
            .ToListAsync();

        if (cellSectorsInMunicipality.Count == 0)
        {
            _logger.LogDebug("No CellSectors found within municipality {MunicipalityId}", municipalityId);
            return null;
        }

        // Now find the nearest CellSector from the filtered set (in-memory Haversine)
        var nearest = cellSectorsInMunicipality
            .Select(cs => new
            {
                cs.Id,
                Distance = CalculateHaversineDistance(latitude, longitude, cs.Latitude, cs.Longitude)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        return nearest?.Id;
    }

    /// <summary>
    /// Finds the nearest CellSector within a specific municipality (hybrid approach).
    /// OPTIMIZED: Uses database query to filter by municipality, then in-memory distance calculation.
    /// More accurate than global nearest approach used in batch processing.
    /// </summary>
    private async Task<Guid?> FindNearestCellSectorInMunicipalityInMemoryAsync(double latitude, double longitude, Guid municipalityId, CancellationToken cancellationToken = default)
    {
        if (_cellSectorCache == null || _cellSectorCache.Count == 0)
            return null;

        // Get CellSector IDs within municipality from database (using spatial index)
        var cellSectorIdsInMunicipality = await _cellSectorRepository
            .Query(cs => !cs.IsDeleted && cs.Cell != null && cs.Cell.Location != null && !cs.Cell.IsDeleted)
            .Include(cs => cs.Cell)
            .Where(cs => _municipalityRepository
                .Query(m => m.Id == municipalityId && m.Boundaries != null && m.Boundaries.Intersects(cs.Cell!.Location))
                .Any())
            .Select(cs => cs.Id)
            .ToHashSetAsync(cancellationToken);

        if (cellSectorIdsInMunicipality.Count == 0)
            return null;

        // Filter cached CellSectors to only those within the municipality, then find nearest
        var nearest = _cellSectorCache
            .Where(cs => cellSectorIdsInMunicipality.Contains(cs.Id))
            .Select(cs => new
            {
                cs.Id,
                Distance = CalculateHaversineDistance(latitude, longitude, cs.Latitude, cs.Longitude)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        return nearest?.Id;
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
