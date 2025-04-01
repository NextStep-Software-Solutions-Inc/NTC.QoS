using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class StudyNetworkRepository(ApplicationDbContext context) : Repository<StudyNetwork>(context), IStudyNetworkRepository
{
    public override async Task<StudyNetwork?> FindAsync(Expression<Func<StudyNetwork, bool>> expression, CancellationToken cancellationToken = default)
    {
        IQueryable<StudyNetwork> query = _context.Set<StudyNetwork>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(sn => sn.Study).ThenInclude(s => s!.AreaStudy)
            .Include(sn => sn.Network).ThenInclude(n => n!.NetworkTemplate)
            .Include(sn => sn.Network).ThenInclude(n => n!.UpLink)
            .Include(sn => sn.Network).ThenInclude(n => n!.DownLink);

        return await _context.Set<StudyNetwork>().FirstOrDefaultAsync(expression, cancellationToken);
    }

    public override async Task<StudyNetwork?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<StudyNetwork>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(sn => sn.Study).ThenInclude(s => s!.AreaStudy)
            .Include(sn => sn.Network).ThenInclude(n => n!.NetworkTemplate)
            .Include(sn => sn.Network).ThenInclude(n => n!.UpLink)
            .Include(sn => sn.Network).ThenInclude(n => n!.DownLink);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    public override async Task<List<StudyNetwork>> ListAsync(
        Expression<Func<StudyNetwork, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StudyNetwork> query = _context.Set<StudyNetwork>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(sn => sn.Study).ThenInclude(s => s!.AreaStudy)
            .Include(sn => sn.Network).ThenInclude(n => n!.NetworkTemplate)
            .Include(sn => sn.Network).ThenInclude(n => n!.UpLink)
            .Include(sn => sn.Network).ThenInclude(n => n!.DownLink);

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return await query.ToListAsync(cancellationToken);
    }
}
