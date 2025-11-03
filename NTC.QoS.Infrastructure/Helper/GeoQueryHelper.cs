using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Common;

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
    /// Gets the center point of a municipality entity.
    /// Returns the pre-calculated center point from the entity's Center property.
    /// </summary>
    /// <param name="entityId">ID of the municipality entity</param>
    /// <param name="repository">Repository for the municipality entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The center point of the entity, or null if entity not found</returns>
    public static async Task<Point?> GetMunicipalityCenterAsync(
        Guid entityId,
        IRepository<Municipality> repository,
        CancellationToken cancellationToken = default)
    {
        var centerQuery = repository.Query(e => e.Id == entityId)
            .Select(e => e.Center);

        return await repository.FirstOrDefaultAsync(centerQuery, cancellationToken);
    }

    /// <summary>
    /// Gets the center point of a province entity.
    /// Returns the pre-calculated center point from the entity's Center property.
    /// </summary>
    /// <param name="entityId">ID of the province entity</param>
    /// <param name="repository">Repository for the province entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The center point of the entity, or null if entity not found</returns>
    public static async Task<Point?> GetProvinceCenterAsync(
        Guid entityId,
        IRepository<Province> repository,
        CancellationToken cancellationToken = default)
    {
        var centerQuery = repository.Query(e => e.Id == entityId)
            .Select(e => e.Center);

        return await repository.FirstOrDefaultAsync(centerQuery, cancellationToken);
    }

    /// <summary>
    /// Gets the center point of a region entity.
    /// Returns the pre-calculated center point from the entity's Center property.
    /// </summary>
    /// <param name="entityId">ID of the region entity</param>
    /// <param name="repository">Repository for the region entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The center point of the entity, or null if entity not found</returns>
    public static async Task<Point?> GetRegionCenterAsync(
        Guid entityId,
        IRepository<Region> repository,
        CancellationToken cancellationToken = default)
    {
        var centerQuery = repository.Query(e => e.Id == entityId)
            .Select(e => e.Center);

        return await repository.FirstOrDefaultAsync(centerQuery, cancellationToken);
    }

    /// <summary>
    /// Gets the center point of a barangay entity.
    /// Returns the pre-calculated center point from the entity's Center property.
    /// </summary>
    /// <param name="entityId">ID of the barangay entity</param>
    /// <param name="repository">Repository for the barangay entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The center point of the entity, or null if entity not found</returns>
    public static async Task<Point?> GetBarangayCenterAsync(
        Guid entityId,
        IRepository<Barangay> repository,
        CancellationToken cancellationToken = default)
    {
        var centerQuery = repository.Query(e => e.Id == entityId)
            .Select(e => e.Center);

        return await repository.FirstOrDefaultAsync(centerQuery, cancellationToken);
    }

    /// <summary>
    /// Gets the center points of multiple municipality entities.
    /// Returns a dictionary mapping entity IDs to their center points.
    /// </summary>
    /// <param name="entityIds">List of municipality entity IDs</param>
    /// <param name="repository">Repository for the municipality entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping entity IDs to their center points</returns>
    public static async Task<Dictionary<Guid, Point>> GetMunicipalityCentersAsync(
        IEnumerable<Guid> entityIds,
        IRepository<Municipality> repository,
        CancellationToken cancellationToken = default)
    {
        var idList = entityIds?.ToList();
        if (idList == null || idList.Count == 0)
            return [];

        var centersQuery = repository.Query(e => idList.Contains(e.Id))
            .Select(e => new { e.Id, e.Center })
            .Where(x => x.Center != null);

        var results = await repository.ToListAsync(centersQuery, cancellationToken);
        
        return results.ToDictionary(x => x.Id, x => x.Center);
    }

    /// <summary>
    /// Gets the center points of multiple province entities.
    /// Returns a dictionary mapping entity IDs to their center points.
    /// </summary>
    /// <param name="entityIds">List of province entity IDs</param>
    /// <param name="repository">Repository for the province entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping entity IDs to their center points</returns>
    public static async Task<Dictionary<Guid, Point>> GetProvinceCentersAsync(
        IEnumerable<Guid> entityIds,
        IRepository<Province> repository,
        CancellationToken cancellationToken = default)
    {
        var idList = entityIds?.ToList();
        if (idList == null || idList.Count == 0)
            return [];

        var centersQuery = repository.Query(e => idList.Contains(e.Id))
            .Select(e => new { e.Id, e.Center })
            .Where(x => x.Center != null);

        var results = await repository.ToListAsync(centersQuery, cancellationToken);
        
        return results.ToDictionary(x => x.Id, x => x.Center);
    }

    /// <summary>
    /// Gets the center points of multiple region entities.
    /// Returns a dictionary mapping entity IDs to their center points.
    /// </summary>
    /// <param name="entityIds">List of region entity IDs</param>
    /// <param name="repository">Repository for the region entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping entity IDs to their center points</returns>
    public static async Task<Dictionary<Guid, Point>> GetRegionCentersAsync(
        IEnumerable<Guid> entityIds,
        IRepository<Region> repository,
        CancellationToken cancellationToken = default)
    {
        var idList = entityIds?.ToList();
        if (idList == null || idList.Count == 0)
            return [];

        var centersQuery = repository.Query(e => idList.Contains(e.Id))
            .Select(e => new { e.Id, e.Center })
            .Where(x => x.Center != null);

        var results = await repository.ToListAsync(centersQuery, cancellationToken);
        
        return results.ToDictionary(x => x.Id, x => x.Center);
    }

    /// <summary>
    /// Gets the center points of multiple barangay entities.
    /// Returns a dictionary mapping entity IDs to their center points.
    /// </summary>
    /// <param name="entityIds">List of barangay entity IDs</param>
    /// <param name="repository">Repository for the barangay entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping entity IDs to their center points</returns>
    public static async Task<Dictionary<Guid, Point>> GetBarangayCentersAsync(
        IEnumerable<Guid> entityIds,
        IRepository<Barangay> repository,
        CancellationToken cancellationToken = default)
    {
        var idList = entityIds?.ToList();
        if (idList == null || idList.Count == 0)
            return [];

        var centersQuery = repository.Query(e => idList.Contains(e.Id))
            .Select(e => new { e.Id, e.Center })
            .Where(x => x.Center != null);

        var results = await repository.ToListAsync(centersQuery, cancellationToken);
        
        return results.ToDictionary(x => x.Id, x => x.Center);
    }

    /// <summary>
    /// Calculates the centroid of a geometry envelope.
    /// This is useful for getting the center point of arbitrary geometries.
    /// </summary>
    /// <param name="geometry">The geometry to calculate centroid for</param>
    /// <returns>The centroid point of the geometry, or null if geometry is invalid</returns>
    public static Point? GetGeometryCentroid(Geometry? geometry)
    {
        if (geometry?.IsValid != true)
            return null;

        try
        {
            return geometry.Centroid;
        }
        catch (Exception)
        {
            // Return null on any centroid calculation errors
            return null;
        }
    }

    /// <summary>
    /// Calculates the centroids of multiple geometry envelopes.
    /// Returns a list of centroid points in the same order as input geometries.
    /// </summary>
    /// <param name="geometries">Collection of geometries to calculate centroids for</param>
    /// <returns>List of centroid points (null for invalid geometries)</returns>
    public static List<Point?> GetGeometryCentroids(IEnumerable<Geometry> geometries)
    {
        if (geometries == null)
            return [];

        return geometries.Select(GetGeometryCentroid).ToList();
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
            foreach (var location in validLocations ?? Enumerable.Empty<Point>())
            {
                var municipality = municipalities.FirstOrDefault(m => m.Boundaries?.Intersects(location) == true);
                result[location] = municipality != null
                    ? (municipality.Id, municipality.Name)
                    : (null, "Unknown");
            }
        }
        catch (Exception)
        {
            // Optionally handle/log the exception here
        }

        return result;
    }

    /// <summary>
    /// Finds all municipalities that intersect with the given geometry, regardless of asset presence.
    /// Returns municipality info with coverage status and asset counts.
    /// </summary>
    /// <param name="geometry">The geometry to search within</param>
    /// <param name="municipalityRepository">Repository for municipality entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping municipality info to their coverage and asset data</returns>
    public static async Task<List<(Guid? Id, string Name, bool IsCovered)>> FindMunicipalitiesWithinGeometryAsync(
        Geometry geometry,
        IMunicipalityRepository municipalityRepository,
        CancellationToken cancellationToken = default)
    {
        if (geometry?.IsValid != true)
        {
            return [];
        }

        try
        {
            // Get all municipalities with boundaries that intersect with the geometry
            var municipalitiesQuery = municipalityRepository
                .Query(m => m.Boundaries != null && m.Boundaries.Intersects(geometry))
                .Select(m => new { m.Id, m.Name });

            var municipalities = await municipalityRepository.ToListAsync(municipalitiesQuery, cancellationToken);

            return municipalities.Select(m => ((Guid?)m.Id, m.Name, true)).ToList();
        }
        catch (Exception)
        {
            // Return empty list on any spatial query errors
            return [];
        }
    }

    // ============================================================================
    // VIEWPORT-OPTIMIZED FILTERING METHODS (V2 API)
    // ============================================================================

    /// <summary>
    /// Applies viewport-based spatial filtering using a rectangular bounding box.
    /// OPTIMIZED for map viewport queries - leverages PostGIS bounding box operator (&&) for index scan.
    /// This is significantly faster than distance-based queries for map rendering scenarios.
    /// 
    /// Performance characteristics:
    /// - Uses spatial index with bounding box operator
    /// - Avoids expensive distance calculations
    /// - Optimized for rectangular viewport areas
    /// - Space efficient: only returns visible entities
    /// 
    /// Use this method for v2 viewport-based APIs to prevent OOM and improve UI performance.
    /// </summary>
    /// <typeparam name="TSource">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="viewport">The viewport bounding box defining the visible map area</param>
    /// <param name="query">The queryable to filter</param>
    /// <returns>Filtered queryable containing only entities within the viewport</returns>
    /// <exception cref="ArgumentNullException">Thrown when viewport is null</exception>
    /// <exception cref="ArgumentException">Thrown when viewport bounds are invalid</exception>
    public static IQueryable<TSource> ApplyViewportFilter<TSource>(
        ViewportBoundsDto viewport,
        IQueryable<TSource> query)
        where TSource : IGeoLocatedEntity
    {
        if (viewport == null)
        {
            throw new ArgumentNullException(nameof(viewport), "Viewport cannot be null for viewport filtering.");
        }

        if (!viewport.IsValid())
        {
            throw new ArgumentException(
                $"Invalid viewport bounds: MinLatitude must be < MaxLatitude and MinLongitude must be < MaxLongitude. " +
                $"Received: [{viewport.MinLatitude}, {viewport.MaxLatitude}] x [{viewport.MinLongitude}, {viewport.MaxLongitude}]",
                nameof(viewport));
        }

        // Create envelope polygon from viewport bounds
        var envelope = GeometryHelper.CreateEnvelopeFromViewport(viewport);

        // Use the existing comprehensive envelope predicate which leverages spatial indexes
        var predicate = SpatialFilterBuilder.BuildComprehensiveEnvelopePredicate<TSource>(envelope);
        return query.Where(predicate);
    }

    /// <summary>
    /// Applies viewport-based spatial filtering with additional boundary entity filtering.
    /// Combines viewport bounds with administrative boundary constraints (Region, Province, Municipality, Barangay).
    /// Use this when the UI needs to filter by both viewport AND administrative boundaries.
    /// </summary>
    /// <typeparam name="TSource">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <typeparam name="TBoundary">The boundary entity type (Region, Province, Municipality, Barangay)</typeparam>
    /// <param name="viewport">The viewport bounding box defining the visible map area</param>
    /// <param name="boundaryIds">List of boundary entity IDs to filter by</param>
    /// <param name="boundaryRepository">Repository for the boundary entities</param>
    /// <param name="query">The queryable to filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered queryable containing entities within both viewport and boundaries</returns>
    public static async Task<IQueryable<TSource>> ApplyViewportWithBoundaryFilter<TSource, TBoundary>(
        ViewportBoundsDto viewport,
        List<Guid> boundaryIds,
        IRepository<TBoundary> boundaryRepository,
        IQueryable<TSource> query,
        CancellationToken cancellationToken)
        where TSource : IGeoLocatedEntity
        where TBoundary : BaseEntity, IGeoBoundaryEntity
    {
        // First apply viewport filter for broad spatial filtering
        query = ApplyViewportFilter(viewport, query);

        // Then apply boundary filter for precise administrative filtering
        if (boundaryIds != null && boundaryIds.Count > 0)
        {
            query = await ApplyBoundaryFilterAsync(boundaryIds, boundaryRepository, query, cancellationToken);
        }

        return query;
    }

    /// <summary>
    /// Applies viewport filtering with service provider filtering.
    /// Optimized for scenarios where both spatial and provider filters are needed.
    /// </summary>
    /// <typeparam name="TSource">The entity type that implements IGeoLocatedEntity and has ServiceProviderId</typeparam>
    /// <param name="viewport">The viewport bounding box</param>
    /// <param name="serviceProviderIds">Optional list of service provider IDs to filter by</param>
    /// <param name="query">The queryable to filter</param>
    /// <returns>Filtered queryable</returns>
    public static IQueryable<TSource> ApplyViewportWithProviderFilter<TSource>(
        ViewportBoundsDto viewport,
        List<Guid>? serviceProviderIds,
        IQueryable<TSource> query)
        where TSource : class, IGeoLocatedEntity
    {
        // Apply viewport filter first (spatial index optimization)
        query = ApplyViewportFilter(viewport, query);

        // Then apply provider filter if specified
        if (serviceProviderIds != null && serviceProviderIds.Count > 0)
        {
            // Use dynamic expression for ServiceProviderId property
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TSource), "entity");
            var property = System.Linq.Expressions.Expression.Property(parameter, "ServiceProviderId");
            var containsMethod = typeof(List<Guid>).GetMethod("Contains", new[] { typeof(Guid) });
            var containsCall = System.Linq.Expressions.Expression.Call(
                System.Linq.Expressions.Expression.Constant(serviceProviderIds),
                containsMethod!,
                property);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<TSource, bool>>(containsCall, parameter);
            query = query.Where(lambda);
        }

        return query;
    }
}
