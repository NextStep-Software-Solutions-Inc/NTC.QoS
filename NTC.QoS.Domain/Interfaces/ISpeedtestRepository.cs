using NTC.QoS.Domain.Entities;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface ISpeedtestRepository : IRepository<Speedtest>
{
    new Task<List<Speedtest>> ListAsync(Expression<Func<Speedtest, bool>>? expression = null, CancellationToken cancellationToken = default);
}
