using NTC.QoS.Domain.Entities;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface INetworkRepository : IRepository<Network>
{
    new Task<List<Network>> ListAsync(Expression<Func<Network, bool>>? expression = null, CancellationToken cancellationToken = default);
}
