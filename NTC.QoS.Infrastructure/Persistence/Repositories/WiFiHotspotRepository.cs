using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class WiFiHotspotRepository(ApplicationDbContext context) 
    : Repository<WiFiHotspot>(context), IWiFiHotspotRepository
{
    public override IQueryable<WiFiHotspot> Query(Expression<Func<WiFiHotspot, bool>>? expression = null)
    {
        var query = _context.Set<WiFiHotspot>()
            .Include(w => w.ServiceProvider)
                .ThenInclude(sp => sp.Provider)
            .Include(w => w.ServiceProvider)
                .ThenInclude(sp => sp.Service)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }

    public async Task<List<WiFiHotspot>> GetActiveHotspotsAsync(CancellationToken cancellationToken = default)
    {
        return await ToListAsync(Query(w => w.IsActive)
            .OrderBy(w => w.Name), cancellationToken);
    }

    public async Task<List<WiFiHotspot>> GetPublicHotspotsAsync(CancellationToken cancellationToken = default)
    {
        return await ToListAsync(Query(w => w.IsPublic && w.IsActive)
            .OrderBy(w => w.Name), cancellationToken);
    }

    public async Task<List<WiFiHotspot>> GetHotspotsByServiceProviderAsync(
        Guid serviceProviderId, 
        CancellationToken cancellationToken = default)
    {
        return await ToListAsync(Query(w => w.ServiceProviderId == serviceProviderId)
            .OrderBy(w => w.Name), cancellationToken);
    }
}