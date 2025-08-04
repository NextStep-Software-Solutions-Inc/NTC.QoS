using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class LinkRepository(ApplicationDbContext context) : Repository<Link>(context), ILinkRepository
{
    public async Task<bool> ValidateLinkTypesAsync(Guid linkId, LinkTypeEnum expectedType, CancellationToken cancellationToken = default)
    {
        var link = await _context.Set<Link>()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => EF.Property<Guid>(l, "Id") == linkId && l.Type == expectedType && !l.IsDeleted, cancellationToken);
        return link is not null;
    }

    public override IQueryable<Link> Query(Expression<Func<Link, bool>>? expression = null)
    {
        var query = _context.Set<Link>()
            .Include(l => l.TechnologyBand)
                .ThenInclude(tb => tb.Band)
            .Include(l => l.TechnologyBand)
                .ThenInclude(tb => tb.Technology)
                    .ThenInclude(t => t.Generation)
            .Include(l => l.ServiceProvider)
                .ThenInclude(sp => sp.Provider)
            .Include(l => l.ServiceProvider)
                .ThenInclude(sp => sp.Service)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }
}
