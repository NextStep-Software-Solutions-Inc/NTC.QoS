using NTC.QoS.Domain.Entities;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IZoneRepository : IRepository<Zone>
{
    new Task<List<Zone>> ListAsync(Expression<Func<Zone, bool>>? expression = null, CancellationToken cancellationToken = default);
}