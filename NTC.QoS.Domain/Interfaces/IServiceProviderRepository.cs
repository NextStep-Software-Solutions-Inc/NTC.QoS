using NTC.QoS.Domain.Aggregates;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IServiceProviderRepository : IRepository<ServiceProvider>
{
    new Task<List<ServiceProvider>> ListAsync(Expression<Func<ServiceProvider, bool>>? expression = null, CancellationToken cancellationToken = default);
}
