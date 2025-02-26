using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;
internal sealed class CellRepository(ApplicationDbContext context) : Repository<Cell>(context), ICellRepository
{
    public override async Task<Cell?> FindAsync(Expression<Func<Cell, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Cell>().Where(expression)
            .Include(c => c.Zone)
            .Include(c => c.ServiceProvider)
            .Include(c => c.File);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public new async Task<List<Cell>> ListAsync(
        Expression<Func<Cell, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Cell> query = _context.Set<Cell>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(c => c.Zone)
            .Include(c => c.ServiceProvider)
            .Include(c => c.File);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await _context.Set<Cell>()
            .Where(expression)
            .Include(c => c.File)
            .Include(c => c.ServiceProvider)
            .Include(c => c.Zone)
            .ToListAsync(cancellationToken);
    }
}
