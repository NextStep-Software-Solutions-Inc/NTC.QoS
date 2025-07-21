using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface IBarangayRepository : IRepository<Barangay>
{
    Task<Barangay?> FindByLocationAsync(double latitude, double longitude);
}