using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.CsvBindings;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service for mapping CSV coverage points to CellularCoverageVersion2 entities
/// </summary>
public interface ICellularCoverageV2MappingService
{
    /// <summary>
    /// Maps a CSV coverage point to a V2 entity
    /// </summary>
    /// <param name="csvRecord">Parsed CSV record</param>
    /// <param name="networkId">User-provided network ID</param>
    /// <param name="bandId">User-provided band ID</param>
    /// <param name="municipalityId">User-provided municipality ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Mapped entity or null if mapping failed/should be skipped</returns>
    Task<CellularCoverageVersion2?> MapCoveragePointAsync(
        CellularCoverageV2CsvRecord csvRecord,
        Guid networkId,
        Guid bandId,
        Guid municipalityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the nearest CellSector to a point within a municipality
    /// </summary>
    /// <param name="latitude">Point latitude</param>
    /// <param name="longitude">Point longitude</param>
    /// <param name="municipalityId">Municipality to search within</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Nearest CellSector ID or null if none found</returns>
    Task<Guid?> FindNearestCellSectorAsync(
        double latitude,
        double longitude,
        Guid municipalityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Derives TechnologyId from NetworkId (based on Network.SystemType)
    /// </summary>
    /// <param name="networkId">Network identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Technology ID or null if not found</returns>
    Task<Guid?> DeriveTechnologyFromNetworkAsync(
        Guid networkId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a duplicate coverage point already exists
    /// </summary>
    /// <param name="latitude">Point latitude</param>
    /// <param name="longitude">Point longitude</param>
    /// <param name="networkId">Network ID</param>
    /// <param name="bandId">Band ID</param>
    /// <param name="municipalityId">Municipality ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if duplicate exists, false otherwise</returns>
    Task<bool> DuplicateExistsAsync(
        double latitude,
        double longitude,
        Guid networkId,
        Guid bandId,
        Guid municipalityId,
        CancellationToken cancellationToken = default);
}
