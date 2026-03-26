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
    
    /// <summary>
    /// Get coverage data by ID with full relationships (excluding boundaries)
    /// </summary>
    Task<BarangayCoverage?> GetByIdWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all coverage data with optional filters, lightweight without relationships
    /// </summary>
    Task<List<BarangayCoverage>> GetAllWithFiltersAsync(
        List<Guid>? regionIds = null,
        List<Guid>? provinceIds = null,
        List<Guid>? municipalityIds = null,
        List<Guid>? barangayIds = null,
        CancellationToken cancellationToken = default);
}
