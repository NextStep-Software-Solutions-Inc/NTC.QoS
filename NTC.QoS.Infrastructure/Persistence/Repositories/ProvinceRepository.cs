using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ProvinceRepository(ApplicationDbContext context) : Repository<Province>(context), IProvinceRepository
{
    public override async Task<Province?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Province>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(p => p.Region);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<List<Province>> ListAsync(
        Expression<Func<Province, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Province> query = _context.Set<Province>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(p => p.Region);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public override async Task<Province?> FindAsync(Expression<Func<Province, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Province>().Where(expression)
            .Include(p => p.Region);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
