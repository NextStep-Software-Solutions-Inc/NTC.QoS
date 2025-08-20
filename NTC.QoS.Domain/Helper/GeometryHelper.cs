using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Helper;

public static class GeometryHelper
{
    private static int SRID = 4326; // WGS 84 spatial reference identifier

    /// <summary>
    /// Creates a Point geometry with the default SRID (4326).
    /// </summary>
    /// <param name="x">The X-coordinate (longitude).</param>
    /// <param name="y">The Y-coordinate (latitude).</param>
    /// <returns>A NetTopologySuite Point object.</returns>
    public static Point CreatePoint(double x, double y)
    {
        return new Point(x, y) { SRID = SRID };
    }

    /// <summary>
    /// Creates a Point geometry from a Coordinate with the default SRID (4326).
    /// </summary>
    /// <param name="coordinate">The Coordinate object.</param>
    /// <returns>A NetTopologySuite Point object.</returns>
    public static Point CreatePoint(Coordinate coordinate)
    {
        return new Point(coordinate) { SRID = SRID };
    }

    /// <summary>
    /// Creates a Polygon geometry with the default SRID (4326).
    /// The shell (exterior ring) must be a closed linear ring (first and last coordinate are the same).
    /// </summary>
    /// <param name="shellCoordinates">The coordinates for the exterior ring of the polygon.</param>
    /// <param name="holeCoordinates">Optional: A collection of collections of coordinates for interior rings (holes).</param>
    /// <returns>A NetTopologySuite Polygon object.</returns>
    public static Polygon CreatePolygon(IEnumerable<Coordinate> shellCoordinates, IEnumerable<IEnumerable<Coordinate>>? holeCoordinates = null)
    {
        if (shellCoordinates == null)
        {
            throw new ArgumentNullException(nameof(shellCoordinates), "Shell coordinates cannot be null for a Polygon.");
        }

        // Ensure the shell is a closed linear ring
        var shell = new LinearRing(shellCoordinates.ToArray());

        List<LinearRing>? holes = null;
        if (holeCoordinates != null)
        {
            holes = new List<LinearRing>();
            foreach (var holeCoords in holeCoordinates)
            {
                if (holeCoords != null)
                {
                    // Ensure each hole is also a closed linear ring
                    holes.Add(new LinearRing(holeCoords.ToArray()));
                }
            }
        }

        return new Polygon(shell, holes?.ToArray()) { SRID = SRID };
    }

    /// <summary>
    /// Creates a MultiPolygon geometry from an array of Polygon objects with the default SRID (4326).
    /// </summary>
    /// <param name="polygons">An array of Polygon objects to be included in the MultiPolygon.</param>
    /// <returns>A NetTopologySuite MultiPolygon object.</returns>
    public static MultiPolygon CreateMultiPolygon(MultiPolygon? boundaries, params Polygon[] polygons)
    {
        if (polygons == null)
        {
            // Return an empty MultiPolygon or throw an exception based on desired behavior
            return new MultiPolygon(new Polygon[] { }) { SRID = SRID };
        }

        // Ensure all constituent polygons also have the correct SRID
        foreach (var polygon in polygons)
        {
            if (polygon != null && polygon.SRID != SRID)
            {
                polygon.SRID = SRID;
            }
        }

        return new MultiPolygon(polygons) { SRID = SRID };
    }

    /// <summary>
    /// Creates a MultiPolygon geometry from a collection of Polygon objects with the default SRID (4326).
    /// </summary>
    /// <param name="polygons">A collection of Polygon objects to be included in the MultiPolygon.</param>
    /// <returns>A NetTopologySuite MultiPolygon object.</returns>
    public static MultiPolygon CreateMultiPolygon(IEnumerable<Polygon> polygons)
    {
        if (polygons == null)
        {
            return new MultiPolygon(new Polygon[] { }) { SRID = SRID };
        }

        var polygonList = new List<Polygon>();
        foreach (var polygon in polygons)
        {
            if (polygon != null)
            {
                if (polygon.SRID != SRID)
                {
                    polygon.SRID = SRID;
                }
                polygonList.Add(polygon);
            }
        }

        return new MultiPolygon(polygonList.ToArray()) { SRID = SRID };
    }
}