using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Helper;

public static class GeoQueryHelper
{
    /// <summary>
    /// Applies spatial filtering based on administrative boundary entities (Region, Province, Municipality, Barangay).
    /// Uses boundary polygons from database entities for comprehensive spatial filtering.
    /// </summary>
    /// <typeparam name="TSource">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <typeparam name="TBoundary">The boundary entity type (Region, Province, Municipality, Barangay)</typeparam>
    /// <param name="ids">List of boundary entity IDs to filter by</param>
    /// <param name="repository">Repository for the boundary entities</param>
    /// <param name="query">The queryable to filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered queryable</returns>
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
    /// Applies comprehensive spatial filtering using an arbitrary geometry envelope.
    /// Includes entities that are contained within OR intersect with the envelope.
    /// This is the RECOMMENDED method for most envelope-based filtering scenarios.
    /// Uses the same spatial logic as boundary filtering for consistency.
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

        var predicate = SpatialFilterBuilder.BuildComprehensiveEnvelopePredicate<TSource>(envelope);
        return query.Where(predicate);
    }

    /// <summary>
    /// Applies comprehensive spatial filtering using multiple geometry envelopes.
    /// Includes entities that are contained within OR intersect with ANY of the provided envelopes.
    /// Uses single enumeration for efficiency.
    /// </summary>
    /// <typeparam name="TSource">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelopes">Collection of geometry envelopes to filter by</param>
    /// <param name="query">The queryable to filter</param>
    /// <returns>Filtered queryable</returns>
    public static IQueryable<TSource> ApplyMultipleEnvelopeFilter<TSource>(
        IEnumerable<Geometry> envelopes,
        IQueryable<TSource> query)
        where TSource : IGeoLocatedEntity
    {
        // Use single enumeration approach for efficiency
        var envelopeList = envelopes?.Where(e => e != null).ToList();
        
        if (envelopeList == null || envelopeList.Count == 0)
            return query;

        var predicate = SpatialFilterBuilder.BuildMultipleEnvelopePredicate<TSource>(envelopeList);
        return query.Where(predicate);
    }

    /// <summary>
    /// Applies spatial filtering with distance tolerance using a geometry envelope.
    /// Includes entities within the envelope OR within the specified distance from the envelope boundary.
    /// </summary>
    /// <typeparam name="TSource">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelope">The geometry envelope to filter by</param>
    /// <param name="distanceToleranceMeters">Distance tolerance in meters for proximity check</param>
    /// <param name="query">The queryable to filter</param>
    /// <returns>Filtered queryable</returns>
    public static IQueryable<TSource> ApplyEnvelopeWithDistanceFilter<TSource>(
        Geometry envelope,
        double distanceToleranceMeters,
        IQueryable<TSource> query)
        where TSource : IGeoLocatedEntity
    {
        if (envelope == null)
            return query;

        var predicate = SpatialFilterBuilder.BuildEnvelopeWithDistancePredicate<TSource>(envelope, distanceToleranceMeters);
        return query.Where(predicate);
    }

    // ============================================================================
    // SPECIALIZED FILTERING METHODS (Use only when specific behavior is needed)
    // ============================================================================

    /// <summary>
    /// Applies STRICT containment filtering - only entities completely within the envelope.
    /// ⚠️  Use this only when you specifically need strict containment (excludes boundary intersections).
    /// For most use cases, prefer ApplyEnvelopeFilter() which is more comprehensive.
    /// </summary>
    /// <typeparam name="TSource">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelope">The geometry envelope to filter by</param>
    /// <param name="query">The queryable to filter</param>
    /// <returns>Filtered queryable with strict containment</returns>
    public static IQueryable<TSource> ApplyStrictContainmentFilter<TSource>(
        Geometry envelope,
        IQueryable<TSource> query)
        where TSource : IGeoLocatedEntity
    {
        if (envelope == null)
            return query;

        var predicate = SpatialFilterBuilder.BuildStrictContainmentPredicate<TSource>(envelope);
        return query.Where(predicate);
    }

    /// <summary>
    /// Applies INTERSECTION-ONLY filtering - only entities that intersect with the envelope boundary.
    /// ⚠️  Use this only when you specifically need intersection detection (excludes contained entities).
    /// For most use cases, prefer ApplyEnvelopeFilter() which is more comprehensive.
    /// </summary>
    /// <typeparam name="TSource">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelope">The geometry envelope to filter by</param>
    /// <param name="query">The queryable to filter</param>
    /// <returns>Filtered queryable with intersection-only</returns>
    public static IQueryable<TSource> ApplyIntersectionOnlyFilter<TSource>(
        Geometry envelope,
        IQueryable<TSource> query)
        where TSource : IGeoLocatedEntity
    {
        if (envelope == null)
            return query;

        var predicate = SpatialFilterBuilder.BuildIntersectionPredicate<TSource>(envelope);
        return query.Where(predicate);
    }

    /// <summary>
    /// Finds the municipality that contains the given location point using spatial intersection.
    /// Optimized for point-in-polygon queries with proper error handling.
    /// </summary>
    /// <param name="location">The point to search for</param>
    /// <param name="municipalityRepository">Repository for municipality entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing municipality ID and name, or null ID with "Unknown" name if not found</returns>
    public static async Task<(Guid? Id, string Name)> FindMunicipalityByLocationAsync(
        Point location,
        IMunicipalityRepository municipalityRepository,
        CancellationToken cancellationToken = default)
    {
        if (location?.IsValid != true)
        {
            return (null, "Unknown");
        }

        try
        {
            // Use spatial intersection to find municipalities that contain the point
            var municipalityQuery = municipalityRepository
                .Query(m => m.Boundaries != null && m.Boundaries.Intersects(location))
                .Select(m => new { m.Id, m.Name });

            var municipality = await municipalityRepository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);

            return municipality != null 
                ? (municipality.Id, municipality.Name)
                : (null, "Unknown");
        }
        catch (Exception)
        {
            // Return unknown on any spatial query errors
            return (null, "Unknown");
        }
    }

    /// <summary>
    /// Finds the municipalities that contain any of the given location points using spatial intersection.
    /// Optimized for batch point-in-polygon queries.
    /// </summary>
    /// <param name="locations">The points to search for</param>
    /// <param name="municipalityRepository">Repository for municipality entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping each location to its municipality info</returns>
    public static async Task<Dictionary<Point, (Guid? Id, string Name)>> FindMunicipalitiesByLocationsAsync(
        IEnumerable<Point> locations,
        IMunicipalityRepository municipalityRepository,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Point, (Guid? Id, string Name)>();
        var validLocations = locations?.Where(l => l?.IsValid == true).ToList();

        if (validLocations?.Count == 0)
        {
            return result;
        }

        try
        {
            // Get all municipalities with boundaries
            var municipalitiesQuery = municipalityRepository
                .Query(m => m.Boundaries != null)
                .Select(m => new { m.Id, m.Name, m.Boundaries });

            var municipalities = await municipalityRepository.ToListAsync(municipalitiesQuery, cancellationToken);

            // Check each location against all municipalities
            foreach (var location in validLocations)
            {
                var municipality = municipalities.FirstOrDefault(m => m.Boundaries?.Intersects(location) == true);
                result[location] = municipality != null 
                    ? (municipality.Id, municipality.Name)
                    : (null, "Unknown");
            }
        }
        catch (Exception)
        {
            // Return unknown for all locations on any errors
            foreach (var location in validLocations)
            {
                result[location] = (null, "Unknown");
            }
        }

        return result;
    }
}
