using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Helper;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class MunicipalityRepository(ApplicationDbContext context) : Repository<Municipality>(context), IMunicipalityRepository
{
    public override IQueryable<Municipality> Query(Expression<Func<Municipality, bool>>? expression = null)
    {
        var query = _context.Set<Municipality>()
            .Include(m => m.Province)
            .ThenInclude(p => p.Region)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;

    }

    /// <summary>
    /// Gets municipality by location using spatial intersection
    /// </summary>
    public async Task<Municipality?> GetMunicipalityByLocationAsync(Point location, CancellationToken cancellationToken = default)
    {
        if (location == null)
            return null;

        return await Query(m => m.Boundaries != null && m.Boundaries.Intersects(location))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets municipality ID and name by location using spatial intersection
    /// </summary>
    public async Task<(Guid? Id, string Name)> GetMunicipalityIdAndNameByLocationAsync(Point location, CancellationToken cancellationToken = default)
    {
        if (location == null)
            return (null, "Unknown");

        var municipality = await Query(m => m.Boundaries != null && m.Boundaries.Intersects(location))
            .Select(m => new { m.Id, m.Name })
            .FirstOrDefaultAsync(cancellationToken);

        return municipality != null
            ? (municipality.Id, municipality.Name)
            : (null, "Unknown");
    }
}
