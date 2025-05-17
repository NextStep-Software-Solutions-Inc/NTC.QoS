
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
}