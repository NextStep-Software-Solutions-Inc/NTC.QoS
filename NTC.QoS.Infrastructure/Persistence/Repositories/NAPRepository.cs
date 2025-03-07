using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;
internal sealed class NAPRepository(ApplicationDbContext context) : Repository<NAP>(context), INAPRepository
{
    public override async Task<NAP?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<NAP>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(n => n.Zone)
            .Include(n => n.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(n => n.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(n => n.File);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<NAP?> FindAsync(Expression<Func<NAP, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<NAP>().Where(expression)
            .Include(n => n.Zone)
            .Include(n => n.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(n => n.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(n => n.File);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<List<NAP>> ListAsync(
        Expression<Func<NAP, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<NAP> query = _context.Set<NAP>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(n => n.Zone)
            .Include(n => n.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(n => n.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(n => n.File);

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }
}
