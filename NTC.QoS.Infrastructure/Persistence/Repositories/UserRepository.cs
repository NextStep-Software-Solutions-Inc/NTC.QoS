using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(ApplicationDbContext context) : Repository<User>(context), IUserRepository
{
    public override IQueryable<User> Query(Expression<Func<User, bool>>? expression = null)
    {
        var query = _context.Set<User>()
            .Include(u => u.Organization)
            .Include(u => u.Role)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }
}