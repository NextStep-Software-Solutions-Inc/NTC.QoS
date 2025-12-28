using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BarangayCoverageRepository(ApplicationDbContext context) 
    : Repository<BarangayCoverage>(context), IBarangayCoverageRepository
{
    public async Task<BarangayCoverage?> GetByBarangayIdAsync(Guid barangayId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BarangayCoverage>()
            .Include(bc => bc.Barangay)
                .ThenInclude(b => b.Municipality)
                    .ThenInclude(m => m.Province)
                        .ThenInclude(p => p.Region)
            .FirstOrDefaultAsync(bc => bc.BarangayId == barangayId, cancellationToken);
    }

    public async Task<List<BarangayCoverage>> GetByRegionIdAsync(Guid regionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BarangayCoverage>()
            .Include(bc => bc.Barangay)
                .ThenInclude(b => b.Municipality)
                    .ThenInclude(m => m.Province)
            .Where(bc => bc.Barangay.Municipality.Province.RegionId == regionId)
            .ToListAsync(cancellationToken);
    }
}
