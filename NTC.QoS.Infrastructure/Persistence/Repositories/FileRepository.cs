using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class FileRepository(ApplicationDbContext context) : Repository<Domain.Entities.File>(context), IFileRepository
{
    public new async Task<Domain.Entities.File?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Domain.Entities.File>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(f => f.Speedtests)
            .Include(f => f.Cellulars)
            .Include(f => f.Cells)
            .Include(f => f.NAPs);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }
    
    public new async Task<List<Domain.Entities.File>> ListAsync(
        Expression<Func<Domain.Entities.File, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Domain.Entities.File> query = _context.Set<Domain.Entities.File>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(f => f.Speedtests)
            .Include(f => f.Cellulars)
            .Include(f => f.Cells)
            .Include(f => f.NAPs);

        if (expression is not null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Applies case-insensitive search filter on file name using EF.Functions.ILike
    /// </summary>
    public IQueryable<Domain.Entities.File> SearchByName(IQueryable<Domain.Entities.File> query, string name)
    {
        return query.Where(f => EF.Functions.ILike(f.Name, $"%{name}%"));
    }
}