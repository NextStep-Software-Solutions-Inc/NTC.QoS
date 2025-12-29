using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Interfaces;

public interface IBarangayCoverageRepository : IRepository<BarangayCoverage>
{
    /// <summary>
    /// Get coverage data for a specific barangay
    /// </summary>
    Task<BarangayCoverage?> GetByBarangayIdAsync(Guid barangayId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all coverage data for barangays in a specific region
    /// </summary>
    Task<List<BarangayCoverage>> GetByRegionIdAsync(Guid regionId, CancellationToken cancellationToken = default);
}
