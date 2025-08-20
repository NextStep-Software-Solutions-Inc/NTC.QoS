using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellularCoverageRepository(ApplicationDbContext context) : Repository<CellularCoverage>(context), ICellularCoverageRepository
{
    public override IQueryable<CellularCoverage> Query(Expression<Func<CellularCoverage, bool>>? expression = null)
    {
        var query = _context.Set<CellularCoverage>()
            .Include(cc => cc.Network)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }
}