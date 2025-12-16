using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class NetworkRepository(ApplicationDbContext context)
    : Repository<Network>(context), INetworkRepository
{
    public IQueryable<Network> SearchByName(IQueryable<Network> query, string name)
    {
        return ApplySearch(query, name, n => n.Name);
    }
}
