using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Repository interface for CellularCoverageVersion2 entity
/// </summary>
public interface ICellularCoverageV2Repository : IRepository<CellularCoverageVersion2>
{
    /// <summary>
    /// Check if a duplicate exists based on location and identifiers
    /// </summary>
    Task<bool> CheckDuplicateAsync(
        double latitude,
        double longitude,
        Guid networkId,
        Guid bandId,
        Guid municipalityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get coverage records by municipality
    /// </summary>
    Task<List<CellularCoverageVersion2>> GetByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken cancellationToken = default);
}
