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
}