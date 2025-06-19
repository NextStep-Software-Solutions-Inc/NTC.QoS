using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ProvinceRepository(ApplicationDbContext context) : Repository<Province>(context), IProvinceRepository
{
    public override IQueryable<Province> Query(Expression<Func<Province, bool>>? expression = null)
    {
        var query = _context.Set<Province>()
            .Include(p => p.Region)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;

    }
}
