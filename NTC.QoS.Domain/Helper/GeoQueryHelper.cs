using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Helper.Spatial;
using NTC.QoS.Domain.Interfaces;

public static class GeoQueryHelper
{
    public static async Task<IQueryable<TSource>> ApplyBoundaryFilterAsync<TSource, TBoundary>(
        List<Guid> ids,
        IGenericGeoRepository<TBoundary> repository,
        IQueryable<TSource> query,
        CancellationToken cancellationToken)
        where TSource : IGeoLocatedEntity
        where TBoundary : class, IGeoBoundaryEntity
    {
        var multipolygons = await repository.Query()
            .Where(entity => ids.Contains(entity.Id) && entity.Boundaries != null)
            .Select(entity => entity.Boundaries!)
            .ToListAsync(cancellationToken);

        if (multipolygons.Count == 0)
            return query;

        var predicate = SpatialFilterBuilder.BuildWithinBoundariesPredicate<TSource>(multipolygons);
        return query.Where(predicate);
    }
}
