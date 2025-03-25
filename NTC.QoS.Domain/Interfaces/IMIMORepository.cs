using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface IMIMORepository : IRepository<MIMO>
{
    Task<MIMO?> GetByIdAsync(Guid? id, CancellationToken cancellationToken = default);
}