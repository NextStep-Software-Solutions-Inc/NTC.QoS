using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class SystemTypeRepository(ApplicationDbContext context) : Repository<SystemType>(context), ISystemTypeRepository
{
    public override IQueryable<SystemType> Query(Expression<Func<SystemType, bool>>? expression = null)
    {
        var query = _context.Set<SystemType>()
            .Include(st => st.Technology)
                .ThenInclude(t => t!.Generation)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }
}
