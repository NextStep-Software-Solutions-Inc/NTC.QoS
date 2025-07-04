using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Helper;

public static class GeoQueryHelper
{
    public static async Task<IQueryable<TSource>> ApplyBoundaryFilterAsync<TSource, TBoundary>(
        List<Guid> ids,
        IRepository<TBoundary> repository,
        IQueryable<TSource> query,
        CancellationToken cancellationToken)
        where TSource : IGeoLocatedEntity
        where TBoundary : BaseEntity, IGeoBoundaryEntity
    {
        var multipolygonsQuery = repository.Query()
            .Where(entity => ids.Contains(entity.Id) && entity.Boundaries != null)
            .Select(entity => entity.Boundaries!);

        var multipolygons = await repository.ToListAsync(multipolygonsQuery, cancellationToken);

        if (multipolygons.Count == 0)
            return query;

        var predicate = SpatialFilterBuilder.BuildWithinBoundariesPredicate<TSource>(multipolygons);
        return query.Where(predicate);
    }
}
