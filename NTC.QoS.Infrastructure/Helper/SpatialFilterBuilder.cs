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
                predicate = predicate.Or(entity => entity.Location.IsWithinDistance(singlePolygon, 0));
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
    /// Combines both intersection and within-boundary checks, similar to the boundaries predicate implementation.
    /// Uses IsWithinDistance(0) which effectively combines containment and intersection checks.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelope">The geometry envelope to filter by</param>
    /// <returns>Expression predicate for spatial filtering</returns>
    public static Expression<Func<T, bool>> BuildComprehensiveEnvelopePredicate<T>(Geometry envelope)
        where T : IGeoLocatedEntity
    {
        if (envelope == null)
            return entity => true;

        // Use IsWithinDistance(0) which matches the pattern used in BuildWithinBoundariesPredicate
        // This effectively includes both contained and intersecting points
        return entity => entity.Location.IsWithinDistance(envelope, 0);
    }

    /// <summary>
    /// Builds a predicate for spatial filtering using multiple geometry envelopes.
    /// Combines both intersection and within-boundary checks for multiple envelopes using the same pattern as boundaries.
    /// </summary>
    /// <typeparam name="T">The entity type that implements IGeoLocatedEntity</typeparam>
    /// <param name="envelopes">Collection of geometry envelopes to filter by</param>
    /// <returns>Expression predicate for spatial filtering</returns>
    public static Expression<Func<T, bool>> BuildMultipleEnvelopePredicate<T>(IEnumerable<Geometry> envelopes)
        where T : IGeoLocatedEntity
    {
        var predicate = PredicateBuilder.New<T>(false);

        foreach (var envelope in envelopes)
        {
            if (envelope != null)
            {
                // Use the same pattern as BuildWithinBoundariesPredicate for consistency
                predicate = predicate.Or(entity => entity.Location.IsWithinDistance(envelope, 0));
            }
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
