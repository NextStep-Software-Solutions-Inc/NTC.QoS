using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellRepository(ApplicationDbContext context) : Repository<Cell>(context), ICellRepository
{
    public override IQueryable<Cell> Query(Expression<Func<Cell, bool>>? expression = null)
    {
        var query = _context.Set<Cell>()
            .Include(c => c.ServiceProvider)
                .ThenInclude(sp => sp!.Provider)
            .Include(c => c.ServiceProvider)
                .ThenInclude(sp => sp!.Service)
            .Include(c => c.CellSectors)
                .ThenInclude(cs => cs!.Cellulars)
                    .ThenInclude(cellular => cellular!.Technology)
                        .ThenInclude(tech => tech!.Generation)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }
}
