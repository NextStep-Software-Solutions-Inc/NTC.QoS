using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class NAPRepository(ApplicationDbContext context) : Repository<NAP>(context), INAPRepository
{
    public override IQueryable<NAP> Query(Expression<Func<NAP, bool>>? expression = null)
    {
        var query = _context.Set<NAP>()
            .Include(c => c.ServiceProvider)
                .ThenInclude(sp => sp!.Provider)
                .ThenInclude(sp => sp!.Service);

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }
}
