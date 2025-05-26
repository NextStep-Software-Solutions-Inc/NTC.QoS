using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellSectorRepository(ApplicationDbContext context) : Repository<CellSector>(context), ICellSectorRepository
{
    public override async Task<CellSector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CellSector>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(cs => cs.Cell)
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    public override async Task<CellSector?> FindAsync(Expression<Func<CellSector, bool>> expression, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CellSector>()
            .Where(expression)
            .Include(cs => cs.Cell)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
