using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class MunicipalityRepository(ApplicationDbContext context) : Repository<Municipality>(context), IMunicipalityRepository
{
    public override IQueryable<Municipality> Query(Expression<Func<Municipality, bool>>? expression = null)
    {
        var query = _context.Set<Municipality>()
            .Include(m => m.Province)
            .ThenInclude(p => p.Region)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;

    }
}
