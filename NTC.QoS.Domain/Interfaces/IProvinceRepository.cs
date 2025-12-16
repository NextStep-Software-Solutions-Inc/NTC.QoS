using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Interfaces;

public interface IProvinceRepository : IRepository<Province>
{
    Task<Dictionary<Guid, int>> GetIspCountsForProvincesAsync(List<Guid> provinceIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top provinces by cell count with optimized spatial queries.
    /// </summary>
    Task<List<GeoEntityCellCountDto>> GetTopProvincesByCellCountAsync(
        Guid? regionId,
    int limit,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the actual count of child locations (municipalities, barangays) for a province based on hierarchical depth.
    /// Used for accurate progress tracking in connectivity assessment jobs.
    /// </summary>
    /// <param name="provinceId">The province ID to count children for</param>
    /// <param name="hierarchicalDepth">
    /// Depth of hierarchy to count:
    /// 1 = Municipalities only
    /// 2 = Municipalities + Barangays
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of child locations at specified depth</returns>
    Task<int> GetChildLocationCountAsync(Guid provinceId, int hierarchicalDepth, CancellationToken cancellationToken = default);
}