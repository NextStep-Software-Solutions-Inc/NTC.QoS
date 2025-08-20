using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class RolePermissionRepository(ApplicationDbContext context) : Repository<RolePermission>(context), IRolePermissionRepository
{
    public override IQueryable<RolePermission> Query(Expression<Func<RolePermission, bool>>? expression = null)
    {
        var query = _context.Set<RolePermission>()
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }
}
