using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface ICellularCoverageRepository : IRepository<CellularCoverage>
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
    Task<List<CellularCoverage>> GetByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken cancellationToken = default);
}
