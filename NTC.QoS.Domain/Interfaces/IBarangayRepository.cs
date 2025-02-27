using NTC.QoS.Domain.Entities;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IBarangayRepository : IRepository<Barangay>
{
    new Task<List<Barangay>> ListAsync(Expression<Func<Barangay, bool>>? expression = null, CancellationToken cancellationToken = default);
}