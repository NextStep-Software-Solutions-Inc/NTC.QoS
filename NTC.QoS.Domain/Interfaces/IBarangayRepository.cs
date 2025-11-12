using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Interfaces;

public interface IBarangayRepository : IRepository<Barangay>
{
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
    
    /// <summary>
    /// Gets top barangays by cell count with optimized spatial queries.
    /// </summary>
    Task<List<GeoEntityCellCountDto>> GetTopBarangaysByCellCountAsync(
        Guid? municipalityId,
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}