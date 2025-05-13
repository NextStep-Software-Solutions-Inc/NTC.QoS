using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;
internal sealed class CellularRepository(ApplicationDbContext context) : Repository<Cellular>(context), ICellularRepository
{
    public override async Task<Cellular?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Cellular>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(c => c.Cell)
            .Include(c => c.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(c => c.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(c => c.GenerationBand).ThenInclude(gb => gb!.Generation)
            .Include(c => c.GenerationBand).ThenInclude(gb => gb!.Band);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<Cellular?> FindAsync(Expression<Func<Cellular, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Cellular>().Where(expression)
            .Include(c => c.Cell)
            .Include(c => c.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(c => c.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(c => c.GenerationBand).ThenInclude(gb => gb!.Generation)
            .Include(c => c.GenerationBand).ThenInclude(gb => gb!.Band);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<List<Cellular>> ListAsync(
        Expression<Func<Cellular, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Cellular> query = _context.Set<Cellular>()
            .Include(c => c.Cell)
            .Include(c => c.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(c => c.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(c => c.GenerationBand).ThenInclude(gb => gb!.Generation)
            .Include(c => c.GenerationBand).ThenInclude(gb => gb!.Band);

        if (expression is not null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }
}
