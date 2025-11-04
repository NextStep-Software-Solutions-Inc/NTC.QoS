using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface IBarangayRepository : IRepository<Barangay>
{
    Task<Barangay?> FindByLocationAsync(double latitude, double longitude);
    
    /// <summary>
    /// Applies case-insensitive search filter on barangay name
    /// </summary>
    IQueryable<Barangay> SearchByName(IQueryable<Barangay> query, string name);
    
    /// <summary>
    /// Applies case-insensitive search filter on municipality name
    /// </summary>
    IQueryable<Barangay> SearchByMunicipalityName(IQueryable<Barangay> query, string municipalityName);
    
    /// <summary>
    /// Applies case-insensitive search filter on province name
    /// </summary>
    IQueryable<Barangay> SearchByProvinceName(IQueryable<Barangay> query, string provinceName);
}