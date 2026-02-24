using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service for mapping cellular coverage points to infrastructure (CellSector and Municipality)
/// </summary>
public interface ICellularCoverageMappingService
{
    /// <summary>
    /// Initializes the mapping service with pre-loaded CellSectors for efficient lookups.
    /// Should be called once before processing a batch of records.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the nearest CellSector and Municipality for a coverage point.
    /// Uses in-memory spatial calculations for performance.
    /// </summary>
    /// <param name="latitude">Latitude of the coverage point</param>
    /// <param name="longitude">Longitude of the coverage point</param>
    /// <returns>Tuple of (CellSectorId, MunicipalityId) or (null, null) if not found</returns>
    Task<(Guid? cellSectorId, Guid? municipalityId)> FindInfrastructureMappingAsync(
        double latitude, 
        double longitude);

    /// <summary>
    /// Batch mapping for multiple coverage points (optimized for bulk processing).
    /// </summary>
    /// <param name="coveragePoints">List of coverage points to map</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping index to (CellSectorId, MunicipalityId)</returns>
    Task<Dictionary<int, (Guid? cellSectorId, Guid? municipalityId)>> FindInfrastructureMappingBatchAsync(
        List<(double latitude, double longitude)> coveragePoints,
        CancellationToken cancellationToken = default);
}
