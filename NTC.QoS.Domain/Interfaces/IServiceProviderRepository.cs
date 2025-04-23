using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IServiceProviderRepository : IRepository<ServiceProvider>
{
    new Task<List<ServiceProvider>> ListAsync(Expression<Func<ServiceProvider, bool>>? expression = null, CancellationToken cancellationToken = default);
    Task<ServiceProvider?> FindByServiceAndProviderNamesAsync(
        string serviceName,
        string providerName,
        CancellationToken cancellationToken = default);
}
