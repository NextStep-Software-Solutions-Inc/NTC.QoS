
using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Helper;
public static class GeometryHelper
{
    private static int SRID = 4326;
    public static Point CreatePoint(double x, double y)
    {
        return new Point(x, y) { SRID = SRID };
    }
    public static Point CreatePoint(Coordinate coordinate)
    {
        return new Point(coordinate) { SRID = SRID };
    }

    /// <summary>
    /// Creates a MultiPolygon from a single Polygon.
    /// This is useful when you need to store a single polygon in a MultiPolygon column.
    /// </summary>
    /// <param name="polygon">The Polygon to wrap in a MultiPolygon.</param>
    /// <returns>A new MultiPolygon containing the provided Polygon, or null if the input is null.</returns>
    public static MultiPolygon? CreateMultiPolygon(Polygon? polygon)
    {
        if (polygon == null)
        {
            return null;
        }

        return new MultiPolygon([polygon]);
    }
}