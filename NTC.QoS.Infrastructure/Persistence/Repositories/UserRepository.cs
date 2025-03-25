using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(ApplicationDbContext context) : Repository<User>(context), IUserRepository
{
    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<User>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(u => u.Role)
            .Include(u => u.Organization);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    public override async Task<List<User>> ListAsync(
        Expression<Func<User, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = _context.Set<User>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(u => u.Role)
            .Include(u => u.Organization);

        if (expression is not null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await ListAsync(null, cancellationToken);
    }
}