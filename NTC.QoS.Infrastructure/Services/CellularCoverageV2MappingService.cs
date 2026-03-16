using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.CsvBindings;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Implementation of CellularCoverage V2 mapping service
/// Maps CSV coverage points to infrastructure (CellSector) within municipalities
/// Optimized for bulk processing with in-memory caching
/// </summary>
public class CellularCoverageV2MappingService : ICellularCoverageV2MappingService
{
    private readonly IRepository<CellularCoverage> _coverageRepository;
    private readonly IRepository<CellSector> _cellSectorRepository;
    private readonly IRepository<Network> _networkRepository;
    private readonly IRepository<Technology> _technologyRepository;
    private readonly ILogger<CellularCoverageV2MappingService> _logger;

    // Cache for network → technology mappings
    private readonly Dictionary<Guid, Guid?> _networkTechnologyCache = new();
    
    // In-memory cache for CellSectors in the current municipality
    private List<CellSectorCache>? _cellSectorCache;
    private Guid? _cachedMunicipalityId;
    
    // Cached technology ID for current network
    private Guid? _cachedTechnologyId;
    private Guid? _cachedNetworkId;

    private class CellSectorCache
    {
        public Guid Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public CellularCoverageV2MappingService(
        IRepository<CellularCoverage> coverageRepository,
        IRepository<CellSector> cellSectorRepository,
        IRepository<Network> networkRepository,
        IRepository<Technology> technologyRepository,
        ILogger<CellularCoverageV2MappingService> logger)
    {
        _coverageRepository = coverageRepository;
        _cellSectorRepository = cellSectorRepository;
        _networkRepository = networkRepository;
        _technologyRepository = technologyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Pre-loads all necessary data for efficient batch processing
    /// </summary>
    public async Task InitializeAsync(Guid networkId, Guid municipalityId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing mapping service for NetworkId={NetworkId}, MunicipalityId={MunicipalityId}", 
            networkId, municipalityId);

        // Load technology for network
        _cachedNetworkId = networkId;
        _cachedTechnologyId = await DeriveTechnologyFromNetworkAsync(networkId, cancellationToken);
        
        if (_cachedTechnologyId == null)
        {
            _logger.LogWarning("Could not derive technology for NetworkId={NetworkId}", networkId);
        }

        // Load all CellSectors for the municipality into memory
        _cachedMunicipalityId = municipalityId;
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
    }

    /// <summary>
    /// Optimized batch processing - maps multiple records at once with minimal DB queries
    /// </summary>
    public async Task<List<CellularCoverage>> MapCoveragePointsBatchAsync(
        List<CellularCoverageV2CsvRecord> csvRecords,
        Guid networkId,
        Guid bandId,
        Guid municipalityId,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CellularCoverage>();

        if (_cachedTechnologyId == null)
        {
            _logger.LogWarning("No technology available for NetworkId={NetworkId}. Batch will be skipped.", networkId);
            return results;
        }

        if (_cellSectorCache == null || _cellSectorCache.Count == 0)
        {
            _logger.LogWarning("No CellSectors available in municipality. Batch will be skipped.");
            return results;
        }

        // Step 1: Filter valid coordinates
        var validRecords = csvRecords
            .Where(r => IsValidCoordinate(r.Latitude, r.Longitude))
            .ToList();

        if (validRecords.Count < csvRecords.Count)
        {
            _logger.LogDebug("Filtered out {Count} invalid coordinates", csvRecords.Count - validRecords.Count);
        }

        // Step 2: Batch duplicate check - get existing points once
        var existingPoints = await GetExistingCoveragePointsAsync(
            validRecords.Select(r => (r.Latitude, r.Longitude)).ToList(),
            networkId,
            bandId,
            municipalityId,
            cancellationToken);

        var existingPointSet = new HashSet<(double lat, double lng)>(
            existingPoints.Select(p => (Math.Round(p.lat, 4), Math.Round(p.lng, 4))));

        // Step 3: Process each record with in-memory lookups
        foreach (var record in validRecords)
        {
            var roundedLat = Math.Round(record.Latitude, 4);
            var roundedLng = Math.Round(record.Longitude, 4);

            // Check duplicate from cached set
            if (existingPointSet.Contains((roundedLat, roundedLng)))
            {
                continue; // Skip duplicate
            }

            // Track accepted points so duplicates within the same CSV batch are skipped too.
            existingPointSet.Add((roundedLat, roundedLng));

            // Find nearest CellSector using in-memory cache
            var cellSectorId = FindNearestCellSectorInMemory(record.Latitude, record.Longitude);
            if (cellSectorId == null)
            {
                continue; // Skip if no nearby cell sector
            }

            // Create entity
            var coveragePoint = new CellularCoverage
            {
                NetworkId = networkId,
                ServiceProviderId = null,  // CSV uploads don't have ServiceProvider
                BandId = bandId,
                TechnologyId = _cachedTechnologyId.Value,
                CellSectorId = cellSectorId.Value,
                MunicipalityId = municipalityId,
                Location = GeometryHelper.CreatePoint(new Coordinate(
                    x: record.Longitude,
                    y: record.Latitude)),
                SignalStrength = record.Signal,
                Address = record.Address,
                IsFileUploaded = true
            };

            results.Add(coveragePoint);
        }

        _logger.LogInformation("Batch processed: {Total} input, {Valid} valid, {Mapped} mapped, {Skipped} skipped",
            csvRecords.Count, validRecords.Count, results.Count, validRecords.Count - results.Count);

        return results;
    }

    /// <summary>
    /// Get existing coverage points for batch duplicate checking
    /// </summary>
    private async Task<List<(double lat, double lng)>> GetExistingCoveragePointsAsync(
        List<(double lat, double lng)> points,
        Guid networkId,
        Guid bandId,
        Guid municipalityId,
        CancellationToken cancellationToken)
    {
        if (points.Count == 0) return new List<(double, double)>();

        try
        {
            const double tolerance = 0.0001;

            // Get min/max bounds for efficient querying
            var minLat = points.Min(p => p.lat) - tolerance;
            var maxLat = points.Max(p => p.lat) + tolerance;
            var minLng = points.Min(p => p.lng) - tolerance;
            var maxLng = points.Max(p => p.lng) + tolerance;

            var existing = await _coverageRepository
                .Query(cc =>
                    !cc.IsDeleted &&
                    cc.NetworkId == networkId &&
                    cc.BandId == bandId &&
                    cc.MunicipalityId == municipalityId &&
                    cc.Location.Y >= minLat && cc.Location.Y <= maxLat &&
                    cc.Location.X >= minLng && cc.Location.X <= maxLng)
                .Select(cc => new { cc.Location.Y, cc.Location.X })
                .ToListAsync(cancellationToken);

            return existing.Select(e => (e.Y, e.X)).ToList();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Duplicate check query was cancelled. Aborting processing to prevent duplicate persistence.");
            throw new InvalidOperationException("Duplicate check was cancelled. Processing aborted to prevent duplicates.", ex);
        }
        catch (Npgsql.NpgsqlException ex) when (ex.SqlState == "57014")
        {
            _logger.LogError(ex, "Duplicate check query timed out (PostgreSQL cancelled). Aborting processing to prevent duplicate persistence.");
            throw new InvalidOperationException("Duplicate check timed out. Processing aborted to prevent duplicates.", ex);
        }
    }

    /// <summary>
    /// Find nearest CellSector using in-memory cache (fast spatial lookup)
    /// </summary>
    private Guid? FindNearestCellSectorInMemory(double latitude, double longitude)
    {
        if (_cellSectorCache == null || _cellSectorCache.Count == 0)
            return null;

        double minDistance = double.MaxValue;
        Guid? nearestId = null;

        foreach (var sector in _cellSectorCache)
        {
            // Calculate Haversine distance (approximate)
            var distance = CalculateDistance(latitude, longitude, sector.Latitude, sector.Longitude);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestId = sector.Id;
            }
        }

        return nearestId;
    }

    /// <summary>
    /// Calculate approximate distance between two points (Haversine formula simplified)
    /// </summary>
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public async Task<CellularCoverage?> MapCoveragePointAsync(
        CellularCoverageV2CsvRecord csvRecord,
        Guid networkId,
        Guid bandId,
        Guid municipalityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Validate coordinates
            if (!IsValidCoordinate(csvRecord.Latitude, csvRecord.Longitude))
            {
                _logger.LogWarning(
                    "Invalid coordinates: Lat={Lat}, Lng={Lng}. Skipping record.",
                    csvRecord.Latitude, csvRecord.Longitude);
                return null;
            }

            // Step 2: Check for duplicates
            var isDuplicate = await DuplicateExistsAsync(
                csvRecord.Latitude,
                csvRecord.Longitude,
                networkId,
                bandId,
                municipalityId,
                cancellationToken);

            if (isDuplicate)
            {
                _logger.LogDebug(
                    "Duplicate coverage point detected at Lat={Lat}, Lng={Lng}. Skipping.",
                    csvRecord.Latitude, csvRecord.Longitude);
                return null;
            }

            // Step 3: Derive technology from network
            var technologyId = await DeriveTechnologyFromNetworkAsync(networkId, cancellationToken);
            if (technologyId == null)
            {
                _logger.LogWarning(
                    "Could not derive technology for NetworkId={NetworkId}. Skipping record.",
                    networkId);
                return null;
            }

            // Step 4: Find nearest CellSector in municipality
            var cellSectorId = await FindNearestCellSectorAsync(
                csvRecord.Latitude,
                csvRecord.Longitude,
                municipalityId,
                cancellationToken);

            if (cellSectorId == null)
            {
                _logger.LogWarning(
                    "No CellSector found near Lat={Lat}, Lng={Lng} in MunicipalityId={MunicipalityId}. Skipping.",
                    csvRecord.Latitude, csvRecord.Longitude, municipalityId);
                return null;
            }

            // Step 5: Create mapped entity
            var coveragePoint = new CellularCoverage
            {
                NetworkId = networkId,
                ServiceProviderId = null,  // CSV uploads don't have ServiceProvider
                BandId = bandId,
                TechnologyId = technologyId.Value,
                CellSectorId = cellSectorId.Value,
                MunicipalityId = municipalityId,
                Location = GeometryHelper.CreatePoint(new Coordinate(
                    x: csvRecord.Longitude,
                    y: csvRecord.Latitude)),
                SignalStrength = csvRecord.Signal,
                Address = csvRecord.Address,
                IsFileUploaded = true
            };

            _logger.LogDebug(
                "Successfully mapped coverage point at Lat={Lat}, Lng={Lng} to CellSectorId={CellSectorId}",
                csvRecord.Latitude, csvRecord.Longitude, cellSectorId);

            return coveragePoint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error mapping coverage point at Lat={Lat}, Lng={Lng}",
                csvRecord.Latitude, csvRecord.Longitude);
            return null;
        }
    }

    public async Task<Guid?> FindNearestCellSectorAsync(
        double latitude,
        double longitude,
        Guid municipalityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var point = GeometryHelper.CreatePoint(new Coordinate(x: longitude, y: latitude));

            // Query CellSectors within the same municipality
            var nearestCellSector = await _cellSectorRepository
                .Query(cs => !cs.IsDeleted)
                .Include(cs => cs.Cell)
                .Where(cs => cs.Cell != null && cs.Cell.Location != null)
                // Filter by municipality through Cell navigation
                .Select(cs => new
                {
                    cs.Id,
                    Distance = cs.Cell!.Location.Distance(point)
                })
                .OrderBy(cs => cs.Distance)
                .FirstOrDefaultAsync(cancellationToken);

            if (nearestCellSector != null)
            {
                _logger.LogDebug(
                    "Found nearest CellSector {CellSectorId} at distance {Distance}m",
                    nearestCellSector.Id, nearestCellSector.Distance);
                return nearestCellSector.Id;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error finding nearest CellSector for point Lat={Lat}, Lng={Lng}, MunicipalityId={MunicipalityId}",
                latitude, longitude, municipalityId);
            return null;
        }
    }

    public async Task<Guid?> DeriveTechnologyFromNetworkAsync(
        Guid networkId,
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_networkTechnologyCache.TryGetValue(networkId, out var cachedTechnologyId))
        {
            return cachedTechnologyId;
        }

        try
        {
            // Fetch network with SystemType
            var network = await _networkRepository
                .Query(n => n.Id == networkId && !n.IsDeleted)
                .Include(n => n.SystemType)
                .ThenInclude(st => st!.Generation)
                .FirstOrDefaultAsync(cancellationToken);

            if (network?.SystemType?.Generation == null)
            {
                _logger.LogWarning("Network {NetworkId} not found or has no SystemType", networkId);
                _networkTechnologyCache[networkId] = null;
                return null;
            }

            // Find technology by generation
            var technology = await _technologyRepository
                .Query(t => t.GenerationId == network.SystemType.Generation.Id && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            var technologyId = technology?.Id;
            _networkTechnologyCache[networkId] = technologyId;

            if (technologyId == null)
            {
                _logger.LogWarning(
                    "No technology found for Network {NetworkId} with Generation {GenerationId}",
                    networkId, network.SystemType.Generation.Id);
            }
            else
            {
                _logger.LogDebug(
                    "Derived TechnologyId={TechnologyId} from NetworkId={NetworkId}",
                    technologyId, networkId);
            }

            return technologyId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deriving technology for NetworkId={NetworkId}", networkId);
            return null;
        }
    }

    public async Task<bool> DuplicateExistsAsync(
        double latitude,
        double longitude,
        Guid networkId,
        Guid bandId,
        Guid municipalityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a small tolerance for coordinate comparison (e.g., 0.0001 degrees ≈ 11 meters)
            const double tolerance = 0.0001;

            var exists = await _coverageRepository
                .Query(cc =>
                    !cc.IsDeleted &&
                    cc.NetworkId == networkId &&
                    cc.BandId == bandId &&
                    cc.MunicipalityId == municipalityId &&
                    Math.Abs(cc.Location.Y - latitude) < tolerance &&
                    Math.Abs(cc.Location.X - longitude) < tolerance)
                .AnyAsync(cancellationToken);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate coverage point");
            return false; // Assume no duplicate on error to allow processing
        }
    }

    /// <summary>
    /// Validates that coordinates are within valid ranges
    /// </summary>
    private static bool IsValidCoordinate(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 &&
               longitude >= -180 && longitude <= 180 &&
               !(Math.Abs(latitude) < 0.0001 && Math.Abs(longitude) < 0.0001); // Not at origin
    }
}
