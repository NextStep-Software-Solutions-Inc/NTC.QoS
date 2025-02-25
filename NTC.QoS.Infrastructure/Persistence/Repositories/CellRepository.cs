using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;
internal sealed class CellRepository(ApplicationDbContext context) : Repository<Cell>(context), ICellRepository
{
    public override async Task<Cell?> FindAsync(Expression<Func<Cell, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Cell>().Where(expression)
            .Include(c => c.Zone);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}