using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Interfaces;

public interface INetworkRepository : IRepository<Network>
{
    /// <summary>
    /// Applies case-insensitive search filter on network name
    /// </summary>
    IQueryable<Network> SearchByName(IQueryable<Network> query, string name);
}
