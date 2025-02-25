using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IMunicipalityRepository : IRepository<Municipality>
{
    new Task<List<Municipality>> ListAsync(Expression<Func<Municipality, bool>>? expression = null, CancellationToken cancellationToken = default);
}