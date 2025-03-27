using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ProviderRepository(ApplicationDbContext context) : Repository<Provider>(context), IProviderRepository
{
    public override async Task<Provider?> FindAsync(Expression<Func<Provider, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Provider>()
            .Where(expression)
            .Include(p => p.ServiceProviders)
            .ThenInclude(sp => sp.Service);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<List<Provider>> ListAsync(Expression<Func<Provider, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Provider>()
            .Where(expression)
            .Include(p => p.ServiceProviders)
            .ThenInclude(sp => sp.Service);
        return await query.ToListAsync(cancellationToken);
    }
}
