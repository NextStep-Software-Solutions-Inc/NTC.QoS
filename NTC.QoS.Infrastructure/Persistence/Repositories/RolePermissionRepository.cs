using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class RolePermissionRepository(ApplicationDbContext context) : Repository<RolePermission>(context), IRolePermissionRepository
{
    public override async Task<RolePermission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<RolePermission>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<List<RolePermission>> ListAsync(
        Expression<Func<RolePermission, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<RolePermission> query = _context.Set<RolePermission>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission);

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }
}
