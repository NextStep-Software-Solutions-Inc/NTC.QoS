using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class TechnologyRepository(ApplicationDbContext context) : Repository<Technology>(context), ITechnologyRepository
{
    public new async Task<Technology?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Technology>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(t => t.Generation);
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public new async Task<List<Technology>> ListAsync(
        Expression<Func<Technology, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Technology> query = _context.Set<Technology>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(t => t.Generation);

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }
}