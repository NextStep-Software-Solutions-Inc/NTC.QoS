using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class TechnologyRepository(ApplicationDbContext context) : Repository<Technology>(context), ITechnologyRepository
{
    public override IQueryable<Technology> Query(Expression<Func<Technology, bool>>? expression = null)
    {
        var query = _context.Set<Technology>()
            .Include(t => t.Generation)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }
}