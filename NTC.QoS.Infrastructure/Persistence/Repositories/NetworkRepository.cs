using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class NetworkRepository(ApplicationDbContext context) 
    : Repository<Network>(context), INetworkRepository
{
    /// <summary>
    /// Applies case-insensitive search filter on network name using EF.Functions.ILike
    /// </summary>
    public IQueryable<Network> SearchByName(IQueryable<Network> query, string name)
    {
        return ApplySearch(query, name, n => n.Name);
    }
}
