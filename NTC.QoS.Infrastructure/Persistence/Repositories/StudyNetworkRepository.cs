using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class StudyNetworkRepository(ApplicationDbContext context) : Repository<StudyNetwork>(context), IStudyNetworkRepository
{
    public override IQueryable<StudyNetwork> Query(Expression<Func<StudyNetwork, bool>>? expression = null)
    {
        var query = _context.Set<StudyNetwork>()
            .Include(sn => sn.Study).ThenInclude(s => s!.AreaStudy)
            .Include(sn => sn.Network).ThenInclude(n => n!.NetworkTemplate)
            .Include(sn => sn.Network).ThenInclude(n => n!.UpLink)
            .Include(sn => sn.Network).ThenInclude(n => n!.DownLink)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }
}
