using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellRepository(ApplicationDbContext context) : Repository<Cell>(context), ICellRepository
{
    public override async Task<Cell?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Cell>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(c => c.Zone).ThenInclude(z => z!.Barangay).ThenInclude(b => b!.Municipality).ThenInclude(m => m!.Province)
            .Include(c => c.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(c => c.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(c => c.File)
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    public override async Task<Cell?> FindAsync(Expression<Func<Cell, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Cell>()
            .Where(expression)
            .Include(c => c.Zone).ThenInclude(z => z!.Barangay).ThenInclude(b => b!.Municipality).ThenInclude(m => m!.Province)
            .Include(c => c.ServiceProvider).ThenInclude(sp => sp!.Service)
            .Include(c => c.ServiceProvider).ThenInclude(sp => sp!.Provider)
            .Include(c => c.File)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
