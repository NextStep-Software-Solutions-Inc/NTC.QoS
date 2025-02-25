using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ServiceRepository(ApplicationDbContext context) : Repository<Service>(context), IServiceRepository
{
    public override async Task<Service?> FindAsync(Expression<Func<Service, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Service>().Where(expression)
            .Include(s => s.ServiceProviders)
            .ThenInclude(sp => sp.Provider);
        

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

}
