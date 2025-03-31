using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface IRegionRepository : IRepository<Region>
{
    Task<List<Region>> GetAllRegionsAsync(CancellationToken cancellationToken = default);
}