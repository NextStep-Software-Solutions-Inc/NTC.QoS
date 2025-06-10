using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class NetworkRepository(ApplicationDbContext context) 
    : Repository<Network>(context), INetworkRepository
{
}

