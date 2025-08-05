using NetTopologySuite.Geometries;
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

    /// <summary>
    /// Applies a spatial filter to the query using an arbitrary geometry envelope (polygon or multipolygon).
    /// Filters entities whose Location is contained within the provided envelope.
    /// </summary>
    /// <typeparam name="TSource">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelope">The geometry envelope (Polygon, MultiPolygon, etc.) to filter by</param>
    /// <param name="query">The queryable to filter</param>
    /// <returns>Filtered queryable</returns>
    public static IQueryable<TSource> ApplyEnvelopeFilter<TSource>(
        Geometry envelope,
        IQueryable<TSource> query)
        where TSource : IGeoLocatedEntity
    {
        if (envelope == null)
            return query;

        // Use Contains for strict inclusion within the envelope
        return query.Where(entity => envelope.Contains(entity.Location));
    }

    /// <summary>
    /// Applies a spatial filter to the query using an arbitrary geometry envelope with intersection.
    /// Filters entities whose Location intersects with the provided envelope.
    /// </summary>
    /// <typeparam name="TSource">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelope">The geometry envelope to filter by</param>
    /// <param name="query">The queryable to filter</param>
    /// <returns>Filtered queryable</returns>
    public static IQueryable<TSource> ApplyEnvelopeIntersectionFilter<TSource>(
        Geometry envelope,
        IQueryable<TSource> query)
        where TSource : IGeoLocatedEntity
    {
        if (envelope == null)
            return query;

        // Use Intersects for entities that touch or overlap the envelope
        return query.Where(entity => envelope.Intersects(entity.Location));
    }
}
