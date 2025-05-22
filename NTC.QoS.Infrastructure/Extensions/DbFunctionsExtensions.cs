using NetTopologySuite.Geometries;

namespace NTC.QoS.Infrastructure.Extensions;

public static class DbFunctionsExtensions
{

    /// <summary>
    /// Maps to the PostGIS ST_ClusterWithinWin function with an optional OVER() clause.
    /// Groups geometries that are within the specified distance of each other
    /// and optionally applies a window function to generate cluster identifiers.
    /// </summary>
    /// <param name="dbFunctions">The EF Core DbFunctions instance.</param>
    /// <param name="geometry">The geometry to cluster.</param>
    /// <param name="distance">The distance threshold for clustering.</param>
    /// <returns>A cluster identifier for each geometry.</returns>
    public static int? ST_ClusterWithinWin(Geometry geometry, double distance)
    {
        throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
    }

    /// <summary>
    /// Maps to the PostGIS ST_ClusterWithin function.
    /// Groups geometries that are within the specified distance of each other.
    /// </summary>
    /// <param name="geometry">The geometry to cluster.</param>
    /// <param name="distance">The distance threshold for clustering.</param>
    /// <returns>A cluster identifier for each geometry.</returns>
    public static GeometryCollection ST_ClusterWithin(Geometry geometry, double distance)
    {
        throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
    }

    /// <summary>
    /// Maps to the PostGIS ST_Centroid function.
    /// Calculates the centroid of a geometry or a collection of geometries.
    /// </summary>
    public static Geometry ST_Centroid(Geometry geometry)
    {
        throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
    }

    /// <summary>
    /// Maps to the PostGIS ST_Collect function.
    /// Aggregates multiple geometries into a single geometry collection.
    /// </summary>
    public static Geometry ST_Collect(IEnumerable<Geometry> geometries)
    {
        throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
    }

    /// <summary>
    /// Maps to the PostGIS ST_ClusterDBSCAN function.
    /// Groups geometries into clusters using the DBSCAN algorithm.
    /// </summary>
    /// <param name="geometry">The geometry to cluster.</param>
    /// <param name="eps">The maximum distance between two points to be considered part of the same cluster.</param>
    /// <param name="minPoints">The minimum number of points required to form a cluster.</param>
    /// <returns>A cluster identifier for each geometry.</returns>
    public static int? ST_ClusterDBSCAN(Geometry geometry, double eps, int minPoints)
    {
        throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
    }
}
