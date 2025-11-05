using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BarangayRepository(ApplicationDbContext context) : Repository<Barangay>(context), IBarangayRepository
{
    /// <summary>
    /// Applies case-insensitive search filter on barangay name using DatabaseSearchHelper
    /// </summary>
    public IQueryable<Barangay> SearchByName(IQueryable<Barangay> query, string name)
    {
        return ApplySearch(query, name, b => b.Name);
    }

    /// <summary>
    /// Applies case-insensitive search filter on municipality name using DatabaseSearchHelper
    /// </summary>
    public IQueryable<Barangay> SearchByMunicipalityName(IQueryable<Barangay> query, string municipalityName)
    {
        return ApplySearch(query, municipalityName, b => b.Municipality.Name);
    }

    /// <summary>
    /// Applies case-insensitive search filter on province name using DatabaseSearchHelper
    /// </summary>
    public IQueryable<Barangay> SearchByProvinceName(IQueryable<Barangay> query, string provinceName)
    {
        return ApplySearch(query, provinceName, b => b.Municipality.Province.Name);
    }
}
