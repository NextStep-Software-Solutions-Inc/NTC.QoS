using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Interfaces;

public interface IRegionRepository : IRepository<Region>
{
    /// <summary>
    /// Gets top regions by cell count with optimized spatial queries.
    /// </summary>
    Task<List<GeoEntityCellCountDto>> GetTopRegionsByCellCountAsync(
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the actual count of child locations (provinces, municipalities, barangays) for a region based on hierarchical depth.
    /// Used for accurate progress tracking in connectivity assessment jobs.
    /// </summary>
    /// <param name="regionId">The region ID to count children for</param>
    /// <param name="hierarchicalDepth">
    /// Depth of hierarchy to count:
    /// 1 = Provinces only
    /// 2 = Provinces + Municipalities
    /// 3 = Provinces + Municipalities + Barangays
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of child locations at specified depth</returns>
    Task<int> GetChildLocationCountAsync(Guid regionId, int hierarchicalDepth, CancellationToken cancellationToken = default);
}