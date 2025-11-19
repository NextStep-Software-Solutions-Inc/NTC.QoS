using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ServiceProviderRepository(ApplicationDbContext context)
    : Repository<ServiceProvider>(context), IServiceProviderRepository
{
    public override IQueryable<ServiceProvider> Query(Expression<Func<ServiceProvider, bool>>? expression = null)
    {
        var query = _context.Set<ServiceProvider>()
            .Include(sp => sp.Provider)
            .Include(sp => sp.Service)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }
}

