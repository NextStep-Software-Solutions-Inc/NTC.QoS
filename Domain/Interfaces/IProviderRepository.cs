using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface IProviderRepository
{
    Task<Provider?> GetByIdAsync(int id);
    Task<List<Provider>> GetAllAsync();
    Task AddAsync(Provider product);
}
