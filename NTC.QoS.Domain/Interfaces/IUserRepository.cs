using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
}