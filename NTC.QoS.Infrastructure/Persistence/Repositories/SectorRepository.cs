using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class SectorRepository(ApplicationDbContext context) 
    : Repository<Sector>(context), ISectorRepository
{
    public override IQueryable<Sector> Query(Expression<Func<Sector, bool>>? expression = null)
    {
        var query = _context.Set<Sector>()
            .Include(s => s.Network)
            .Include(s => s.MIMO)
            .Include(s => s.CellSector)
                .ThenInclude(cs => cs.Cell)
            .Include(s => s.TxAntennaPattern)
            .Include(s => s.RxAntennaPattern)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }
}
