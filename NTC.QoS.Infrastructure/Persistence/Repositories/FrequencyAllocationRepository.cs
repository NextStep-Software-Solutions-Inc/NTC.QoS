using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class FrequencyAllocationRepository(ApplicationDbContext context)
    : Repository<FrequencyAllocation>(context), IFrequencyAllocationRepository
{
    public override IQueryable<FrequencyAllocation> Query(Expression<Func<FrequencyAllocation, bool>>? expression = null)
    {
        var query = _context.Set<FrequencyAllocation>()
            .Include(fa => fa.Provider)
            .Include(fa => fa.UpLink)
                .ThenInclude(l => l.Band)
            .Include(fa => fa.UpLink)
                .ThenInclude(l => l.ServiceProvider)
                    .ThenInclude(sp => sp.Provider)
            .Include(fa => fa.UpLink)
                .ThenInclude(l => l.ServiceProvider)
                    .ThenInclude(sp => sp.Service)
            .Include(fa => fa.DownLink)
                .ThenInclude(l => l.Band)
            .Include(fa => fa.DownLink)
                .ThenInclude(l => l.ServiceProvider)
                    .ThenInclude(sp => sp.Provider)
            .Include(fa => fa.DownLink)
                .ThenInclude(l => l.ServiceProvider)
                    .ThenInclude(sp => sp.Service)
            .AsQueryable();

        if (expression is not null)
            query = query.Where(expression);

        return query;
    }
}
