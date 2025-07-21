using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellularRepository(ApplicationDbContext context) : Repository<Cellular>(context), ICellularRepository
{
    public override IQueryable<Cellular> Query(Expression<Func<Cellular, bool>>? expression = null)
    {
        var query = _context.Set<Cellular>()
            .Include(c => c.GenerationBand)
                .ThenInclude(gb => gb!.Band)
            .Include(c => c.GenerationBand)
                .ThenInclude(gb => gb!.Generation)
            .Include(c => c.ServiceProvider)
                .ThenInclude(sp => sp!.Provider)
            .Include(c => c.ServiceProvider)
                .ThenInclude(sp => sp!.Service)
            .Include(c => c.CellSector)
                .ThenInclude(cs => cs!.Cell)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }
}
