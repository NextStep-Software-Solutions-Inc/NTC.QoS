using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Interfaces;

public interface IMunicipalityRepository : IRepository<Municipality>
{
    Task<Municipality?> GetMunicipalityByLocationAsync(Point location, CancellationToken cancellationToken = default);
    Task<(Guid? Id, string Name)> GetMunicipalityIdAndNameByLocationAsync(Point location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top municipalities by cell count with optimized spatial queries.
    /// </summary>
    Task<List<GeoEntityCellCountDto>> GetTopMunicipalitiesByCellCountAsync(
         Guid? provinceId,
           int limit,
           DateTime? startDate = null,
           DateTime? endDate = null,
           CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the actual count of child locations (barangays) for a municipality.
    /// Used for accurate progress tracking in connectivity assessment jobs.
    /// </summary>
    /// <param name="municipalityId">The municipality ID to count children for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of barangays in the municipality</returns>
    Task<int> GetChildLocationCountAsync(Guid municipalityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the full Municipality entity (including Boundaries and Description) for update operations,
    /// bypassing the read-optimized projection used by Query().
    /// </summary>
    Task<Municipality?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}