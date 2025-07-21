using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellSectorRepository(ApplicationDbContext context) : Repository<CellSector>(context), ICellSectorRepository
{
    public override IQueryable<CellSector> Query(Expression<Func<CellSector, bool>>? expression = null)
    {
        var query = _context.Set<CellSector>()
            .Include(cs => cs.Cell)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }
}
