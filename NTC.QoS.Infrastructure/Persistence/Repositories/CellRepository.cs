using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellRepository(ApplicationDbContext context) : Repository<Cell>(context), ICellRepository
{
    public override IQueryable<Cell> Query(Expression<Func<Cell, bool>>? expression = null)
    {
        var query = _context.Set<Cell>()
            .Include(c => c.ServiceProvider)
                .ThenInclude(sp => sp.Provider)
            .Include(c => c.ServiceProvider)
                .ThenInclude(sp => sp.Service)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }
}
