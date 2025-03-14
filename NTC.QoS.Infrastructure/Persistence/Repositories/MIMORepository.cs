using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class MIMORepository(ApplicationDbContext context) : Repository<MIMO>(context), IMIMORepository
{
    public async Task<MIMO?> GetByIdAsync(Guid? id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<MIMO>().Where(e => !EF.Property<bool>(e, "IsDeleted"));
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<List<MIMO>> ListAsync(
        Expression<Func<MIMO, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<MIMO> query = _context.Set<MIMO>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"));
        if (expression != null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }
}