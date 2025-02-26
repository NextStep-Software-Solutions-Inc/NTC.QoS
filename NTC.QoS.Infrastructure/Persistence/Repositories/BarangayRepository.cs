using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BarangayRepository(ApplicationDbContext context) : Repository<Barangay>(context), IBarangayRepository
{
    public new async Task<Barangay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Barangay>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(b => b.Municipality);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public new async Task<List<Barangay>> ListAsync(
        Expression<Func<Barangay, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Barangay> query = _context.Set<Barangay>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(b => b.Municipality);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
