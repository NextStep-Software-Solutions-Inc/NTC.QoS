using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class TechnologyBandRepository(ApplicationDbContext context) : Repository<TechnologyBand>(context), ITechnologyBandRepository
{
    public override IQueryable<TechnologyBand> Query(Expression<Func<TechnologyBand, bool>>? expression = null)
    {
        var query = _context.Set<TechnologyBand>()
            .Include(tb => tb.Band)
            .Include(tb => tb.Technology)
                .ThenInclude(t => t.Generation)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }
}
