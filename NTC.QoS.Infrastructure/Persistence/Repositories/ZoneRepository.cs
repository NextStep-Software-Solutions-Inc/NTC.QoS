using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ZoneRepository(ApplicationDbContext context) : Repository<Zone>(context), IZoneRepository
{
    public override async Task<Zone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Zone>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(z => z.Barangay)
                .ThenInclude(b => b!.Municipality)
                    .ThenInclude(m => m!.Province)
                        .ThenInclude(p => p!.Region);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<List<Zone>> ListAsync(
        Expression<Func<Zone, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Zone> query = _context.Set<Zone>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(z => z.Barangay)
                .ThenInclude(b => b!.Municipality)
                    .ThenInclude(m => m!.Province)
                        .ThenInclude(p => p!.Region);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public override async Task<Zone?> FindAsync(Expression<Func<Zone, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Zone>().Where(expression)
           .Include(z => z.Barangay).ThenInclude(z => z!.Municipality).ThenInclude(m => m!.Province).ThenInclude(p => p!.Region);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
