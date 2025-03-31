using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class NetworkRepository(ApplicationDbContext context) 
    : Repository<Network>(context), INetworkRepository
{
    public override async Task<Network?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Network>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(n => n.NetworkTemplate)
            .Include(n => n.UpLink).ThenInclude(u => u!.Band)
            .Include(n => n.DownLink).ThenInclude(d => d!.Band);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    public override async Task<List<Network>> ListAsync(
        Expression<Func<Network, bool>>? expression = null, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<Network> query = _context.Set<Network>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted")) 
            .Include(n => n.NetworkTemplate)
            .Include(n => n.UpLink).ThenInclude(u => u!.Band)
            .Include(n => n.DownLink).ThenInclude(d => d!.Band);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }
}

