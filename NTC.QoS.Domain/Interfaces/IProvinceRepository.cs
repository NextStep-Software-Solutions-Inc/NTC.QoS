using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface IProvinceRepository: IRepository<Province>
{
    Task<Dictionary<Guid, int>> GetIspCountsForProvincesAsync(List<Guid> provinceIds, CancellationToken cancellationToken = default);
}