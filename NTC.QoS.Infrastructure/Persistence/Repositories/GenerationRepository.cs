using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class GenerationRepository(ApplicationDbContext context) : Repository<Generation>(context), IGenerationRepository
{
    public new async Task<Generation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Generation>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(g => g.Technologies);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public new async Task<List<Generation>> ListAsync(
        Expression<Func<Generation, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Generation> query = _context.Set<Generation>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(g => g.Technologies);

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }
}