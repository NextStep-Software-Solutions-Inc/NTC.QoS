using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class LinkRepository(ApplicationDbContext context) : Repository<Link>(context), ILinkRepository
{
    public new async Task<Link?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Link>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(l => l.ServiceProvider)
            .Include(l => l.Band);
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public new async Task<List<Link>> ListAsync(
        Expression<Func<Link, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Link> query = _context.Set<Link>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(l => l.ServiceProvider)
            .Include(l => l.Band);

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }
}