using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class NAPRepository(ApplicationDbContext context) : Repository<NAP>(context), INAPRepository
{
    public override IQueryable<NAP> QueryAsNoTracking(Expression<Func<NAP, bool>>? expression = null)
    {
        var query = _context.Set<NAP>()
            .Include(c => c.ServiceProvider)
                .ThenInclude(sp => sp!.Provider)
            .Include(c => c.ServiceProvider)
                .ThenInclude(sp => sp!.Service)
            .AsNoTracking();

        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }

    public IQueryable<NAP> QueryNAPsWithinBoundaries(
        List<Guid>? regionIds = null,
        List<Guid>? provinceIds = null,
        List<Guid>? municipalityIds = null,
        List<Guid>? barangayIds = null)
    {
        var query = QueryAsNoTracking();

        if (regionIds is not null && regionIds.Count > 0)
        {
            var regionBoundariesQuery = _context.Regions
                .Where(r => regionIds.Contains(r.Id))
                .Select(r => r.Boundaries);

            query = query.Where(nap => regionBoundariesQuery.Any(boundary => boundary != null && boundary.Intersects(nap.Location)));
        }
        else if (provinceIds is not null && provinceIds.Count > 0)
        {
            var provinceBoundariesQuery = _context.Provinces
                .Where(p => provinceIds.Contains(p.Id))
                .Select(p => p.Boundaries);

            query = query.Where(nap => provinceBoundariesQuery.Any(boundary => boundary != null && boundary.Intersects(nap.Location)));
        }
        else if (municipalityIds is not null && municipalityIds.Count > 0)
        {
            var municipalityBoundariesQuery = _context.Municipalities
                .Where(m => municipalityIds.Contains(m.Id))
                .Select(m => m.Boundaries);

            query = query.Where(nap => municipalityBoundariesQuery.Any(boundary => boundary != null && boundary.Intersects(nap.Location)));
        }
        else if (barangayIds is not null && barangayIds.Count > 0)
        {
            var barangayBoundariesQuery = _context.Barangays
                .Where(b => barangayIds.Contains(b.Id))
                .Select(b => b.Boundaries);

            query = query.Where(nap => barangayBoundariesQuery.Any(boundary => boundary != null && boundary.Intersects(nap.Location)));
        }

        return query;
    }
}
