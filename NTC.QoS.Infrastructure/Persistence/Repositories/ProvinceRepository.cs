using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ProvinceRepository(ApplicationDbContext context) : Repository<Province>(context), IProvinceRepository
{
    public async Task<Dictionary<Guid, int>> GetIspCountsForProvincesAsync(List<Guid> provinceIds, CancellationToken cancellationToken = default)
    {
        var result = await _context.Set<Province>()
            .Where(p => provinceIds.Contains(p.Id))
            .Select(p => new
            {
                ProvinceId = p.Id,
                IspCount = _context.Set<Cell>()
                    .Where(c => c.Location.Intersects(p.Boundaries))
                    .Select(c => c.ServiceProviderId)
                    .Concat(
                        _context.Set<NAP>()
                            .Where(n => n.Location.Intersects(p.Boundaries))
                            .Select(n => n.ServiceProviderId)
                    )
                    .Distinct()
                    .Count()
            })
            .ToDictionaryAsync(x => x.ProvinceId, x => x.IspCount, cancellationToken);

        return result;
    }
}
