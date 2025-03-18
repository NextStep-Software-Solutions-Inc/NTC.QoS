using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class MunicipalityRepository(ApplicationDbContext context) : Repository<Municipality>(context), IMunicipalityRepository
{
    public new async Task<Municipality?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Municipality>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(m => m.Province)
                .ThenInclude(p => p!.Region);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public new async Task<List<Municipality>> ListAsync(
        Expression<Func<Municipality, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Municipality> query = _context.Set<Municipality>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(m => m.Province)
                .ThenInclude(p => p!.Region);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
