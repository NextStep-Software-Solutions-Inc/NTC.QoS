using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Models.DTOs.GeoJson;

namespace NTC.QoS.Domain.Helper;

public static class GeoJsonHelper
{
    public static GeoJsonFeatureDto? BuildFeature(MultiPolygon? mp, IDictionary<string, object>? properties = null)
    {
        if (mp == null)
        {
            return null;
        }

        var coordinates = new List<List<List<double[]>>>();

        for (int i = 0; i < mp.NumGeometries; i++)
        {
            var polygon = mp.GetGeometryN(i) as Polygon;
            if (polygon == null)
            {
                continue;
            }

            var rings = new List<List<double[]>>
            {
                // Exterior ring
                ToLngLatArray(polygon.ExteriorRing.Coordinates)
            };

            // Interior rings (holes)
            for (int r = 0; r < polygon.NumInteriorRings; r++)
            {
                var interior = polygon.GetInteriorRingN(r);
                rings.Add(ToLngLatArray(interior.Coordinates));
            }

            coordinates.Add(rings);
        }

        var geometry = new GeoJsonGeometryDto
        {
            Type = "MultiPolygon",
            Coordinates = coordinates
        };

        return new GeoJsonFeatureDto
        {
            Geometry = geometry,
            Properties = properties != null ? new Dictionary<string, object>(properties) : null
        };
    }

    private static List<double[]> ToLngLatArray(Coordinate[] coords)
    {
        var list = new List<double[]>(coords.Length);
        foreach (var c in coords)
        {
            // GeoJSON expects [longitude, latitude]
            list.Add([c.X, c.Y]);
        }
        return list;
    }
}
