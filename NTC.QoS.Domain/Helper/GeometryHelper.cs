using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Helper;
public static class GeometryHelper
{
    public static Point CreatePoint(double x, double y)
    {
        return new Point(x, y) { SRID = 4326 };
    }
    public static Point CreatePoint(Coordinate coordinate)
    {
        return new Point(coordinate) { SRID = 4326 };
    }
}