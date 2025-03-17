using NTC.QoS.Domain.Entities;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface ISectorRepository: IRepository<Sector>
{
    Task<List<Sector>> GetAllSectorsAsync(CancellationToken cancellationToken = default);
    Task<List<Sector>> FindSectorsAsync(Expression<Func<Sector, bool>> expression, CancellationToken cancellationToken = default);
}