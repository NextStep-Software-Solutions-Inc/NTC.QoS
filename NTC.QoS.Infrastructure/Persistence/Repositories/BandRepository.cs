using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BandRepository(ApplicationDbContext context) : Repository<Band>(context), IBandRepository
{
    public override async Task<Band?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Band>().Where(e => !EF.Property<bool>(e, "IsDeleted"));
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public override async Task<List<Band>> ListAsync(
        Expression<Func<Band, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Band> query = _context.Set<Band>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"));
        if (expression != null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }
}