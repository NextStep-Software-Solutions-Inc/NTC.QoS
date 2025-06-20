using System.Linq.Expressions;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BarangayRepository(ApplicationDbContext context) : Repository<Barangay>(context), IBarangayRepository
{
    public override IQueryable<Barangay> Query(Expression<Func<Barangay, bool>>? expression = null)
    {
        var query = _context.Set<Barangay>()
            .Include(b => b.Municipality)
                .ThenInclude(m => m.Province)
                    .ThenInclude(p => p.Region)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;

    }
}
