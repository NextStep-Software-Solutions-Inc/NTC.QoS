using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ServiceProviderRepository(ApplicationDbContext context) 
    : Repository<ServiceProvider>(context), IServiceProviderRepository
{
    public override async Task<ServiceProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ServiceProvider>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(sp => sp.Service)
            .Include(sp => sp.Provider);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<List<ServiceProvider>> ListAsync(
        Expression<Func<ServiceProvider, bool>>? expression = null, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<ServiceProvider> query = _context.Set<ServiceProvider>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted")) // Exclude deleted items
            .Include(sp => sp.Service)  
            .Include(sp => sp.Provider); 

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }
}

