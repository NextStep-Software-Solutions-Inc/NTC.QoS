using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class SpeedtestRepository(ApplicationDbContext context)
    : Repository<Speedtest>(context), ISpeedtestRepository
{
    public override IQueryable<Speedtest> Query(Expression<Func<Speedtest, bool>>? expression = null)
    {
        var query = _context.Set<Speedtest>()
            .Include(st => st.ServiceProvider)
                .ThenInclude(sp => sp.Provider)
            .Include(st => st.ServiceProvider)
                .ThenInclude(sp => sp.Service)
            .Include(st => st.Generation)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }
}

