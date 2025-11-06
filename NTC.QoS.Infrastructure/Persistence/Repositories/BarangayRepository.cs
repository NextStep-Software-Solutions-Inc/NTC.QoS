using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BarangayRepository(ApplicationDbContext context) : Repository<Barangay>(context), IBarangayRepository
{
    public IQueryable<Barangay> SearchByName(IQueryable<Barangay> query, string name)
    {
        return ApplySearch(query, name, b => b.Name);
    }

    public IQueryable<Barangay> SearchByMunicipalityName(IQueryable<Barangay> query, string municipalityName)
    {
        return ApplySearch(query, municipalityName, b => b.Municipality.Name);
    }

    public IQueryable<Barangay> SearchByProvinceName(IQueryable<Barangay> query, string provinceName)
    {
        return ApplySearch(query, provinceName, b => b.Municipality.Province.Name);
    }
}
