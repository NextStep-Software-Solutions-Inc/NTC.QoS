using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class StudyRepository(ApplicationDbContext context) 
    : Repository<Study>(context), IStudyRepository
{
    public new async Task<Study?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Study>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(sp => sp.AreaStudy);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    public new async Task<List<Study>> ListAsync(
        Expression<Func<Study, bool>>? expression = null, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<Study> query = _context.Set<Study>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(sp => sp.AreaStudy);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }
}

