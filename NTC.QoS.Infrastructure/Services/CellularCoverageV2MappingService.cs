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
/// </summary>
public class CellularCoverageV2MappingService : ICellularCoverageV2MappingService
{
    private readonly IRepository<CellularCoverageVersion2> _coverageRepository;
    private readonly IRepository<CellSector> _cellSectorRepository;
    private readonly IRepository<Network> _networkRepository;
    private readonly IRepository<Technology> _technologyRepository;
    private readonly ILogger<CellularCoverageV2MappingService> _logger;

    // Cache for network → technology mappings
    private readonly Dictionary<Guid, Guid?> _networkTechnologyCache = new();

    public CellularCoverageV2MappingService(
        IRepository<CellularCoverageVersion2> coverageRepository,
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

    public async Task<CellularCoverageVersion2?> MapCoveragePointAsync(
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
            var coveragePoint = new CellularCoverageVersion2
            {
                NetworkId = networkId,
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
