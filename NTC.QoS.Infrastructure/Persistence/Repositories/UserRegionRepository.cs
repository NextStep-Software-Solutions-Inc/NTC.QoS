using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class UserRegionRepository(ApplicationDbContext context) : Repository<UserRegion>(context), IUserRegionRepository
{
    public override IQueryable<UserRegion> Query(Expression<Func<UserRegion, bool>>? expression = null)
    {
        var query = _context.Set<UserRegion>()
            .Include(ur => ur.User)
                .ThenInclude(u => u.Role)
            .Include(ur => ur.User)
                .ThenInclude(u => u.Organization)
            .Include(ur => ur.Region)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }
}
