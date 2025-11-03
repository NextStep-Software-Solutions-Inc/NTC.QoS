using LinqKit;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Helper;

public static class SpatialFilterBuilder
{
    public static Expression<Func<T, bool>> BuildWithinBoundariesPredicate<T>(IEnumerable<MultiPolygon> multipolygons)
        where T : IGeoLocatedEntity
    {
        var predicate = PredicateBuilder.New<T>(false);

        foreach (var multiPolygon in multipolygons)
        {
            // Loop through each individual polygon in the MultiPolygon
            for (int i = 0; i < multiPolygon.NumGeometries; i++)
            {
                var singlePolygon = multiPolygon.GetGeometryN(i);
                // Use Intersects instead of Within for geography compatibility
                // For points, Intersects is equivalent to Within (point inside polygon)
                predicate = predicate.Or(entity => singlePolygon.Intersects(entity.Location));
            }
        }

        return predicate;
    }

    public static Expression<Func<T, bool>> BuildWithinDistancePredicate<T>(Point center, double distance)
        where T : IGeoLocatedEntity
    {
        return entity => entity.Location.IsWithinDistance(center, distance);
    }

    /// <summary>
    /// Builds a comprehensive predicate for spatial filtering using a geometry envelope.
    /// Uses Intersects for geography compatibility instead of Within.
    /// For viewport queries, we want points that are inside the bounding box.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelope">The geometry envelope to filter by</param>
    /// <returns>Expression predicate for spatial filtering</returns>
    public static Expression<Func<T, bool>> BuildComprehensiveEnvelopePredicate<T>(Geometry envelope)
        where T : IGeoLocatedEntity
    {
        if (envelope == null)
            return entity => true;

        // Use Intersects for geography compatibility - for points, equivalent to Within
        return entity => envelope.Intersects(entity.Location);
    }

    /// <summary>
    /// Builds a predicate for spatial filtering using multiple geometry envelopes.
    /// Uses Intersects for fast bounding box queries instead of distance calculations.
    ///
    /// Behavior:
    /// - If envelopes collection is null or empty, returns a predicate that matches no entities (false)
    /// - If envelopes collection contains only null values, returns a predicate that matches no entities (false)
    /// - If envelopes collection contains valid geometries, returns a predicate that matches entities within any of the envelopes
    /// - OPTIMIZED: Avoids materializing geometries to reduce memory usage and DB roundtrips
    /// </summary>
    /// <typeparam name="T">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelopes">Collection of geometry envelopes to filter by</param>
    /// <returns>Expression predicate for spatial filtering</returns>
    public static Expression<Func<T, bool>> BuildMultipleEnvelopePredicate<T>(IEnumerable<Geometry> envelopes)
        where T : IGeoLocatedEntity
    {
        var predicate = PredicateBuilder.New<T>(false);

        // Filter out null/invalid values without materializing the entire collection
        var validEnvelopes = envelopes?.Where(e => e != null && e.IsValid);

        if (validEnvelopes == null || !validEnvelopes.Any())
        {
            // Return a predicate that matches no entities when collection is empty or contains only nulls/invalids
            return predicate;
        }

        // Build OR predicate for each valid envelope without materializing
        foreach (var envelope in validEnvelopes)
        {
            // Use Intersects for geography compatibility - for points, equivalent to Within
            predicate = predicate.Or(entity => envelope.Intersects(entity.Location));
        }

        return predicate;
    }

    /// <summary>
    /// Builds a predicate for spatial filtering using a geometry envelope with distance tolerance.
    /// Combines containment, intersection, and proximity checks.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelope">The geometry envelope to filter by</param>
    /// <param name="distanceTolerance">Distance tolerance in meters for proximity check</param>
    /// <returns>Expression predicate for spatial filtering</returns>
    public static Expression<Func<T, bool>> BuildEnvelopeWithDistancePredicate<T>(Geometry envelope, double distanceTolerance)
        where T : IGeoLocatedEntity
    {
        if (envelope == null)
            return entity => true;

        // Use IsWithinDistance with the specified tolerance
        return entity => entity.Location.IsWithinDistance(envelope, distanceTolerance);
    }

    /// <summary>
    /// Builds a predicate for strict containment filtering using a geometry envelope.
    /// Only includes entities whose Location is completely contained within the envelope.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelope">The geometry envelope to filter by</param>
    /// <returns>Expression predicate for strict containment filtering</returns>
    public static Expression<Func<T, bool>> BuildStrictContainmentPredicate<T>(Geometry envelope)
        where T : IGeoLocatedEntity
    {
        if (envelope == null)
            return entity => true;

        // Use Contains for strict inclusion only
        return entity => envelope.Contains(entity.Location);
    }

    /// <summary>
    /// Builds a predicate for intersection filtering using a geometry envelope.
    /// Only includes entities whose Location intersects with the envelope.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelope">The geometry envelope to filter by</param>
    /// <returns>Expression predicate for intersection filtering</returns>
    public static Expression<Func<T, bool>> BuildIntersectionPredicate<T>(Geometry envelope)
        where T : IGeoLocatedEntity
    {
        if (envelope == null)
            return entity => true;

        // Use Intersects for intersection checks
        return entity => envelope.Intersects(entity.Location);
    }
}
