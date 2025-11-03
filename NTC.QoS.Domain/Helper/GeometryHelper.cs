using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Models.DTOs.Common;

namespace NTC.QoS.Domain.Helper;

public static class GeometryHelper
{
    private static readonly int SRID = 4326; // WGS 84 spatial reference identifier

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
            holes = [];
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
            return new MultiPolygon([]) { SRID = SRID };
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
            return new MultiPolygon([]) { SRID = SRID };
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

        return new MultiPolygon([.. polygonList]) { SRID = SRID };
    }

    /// <summary>
    /// Creates a rectangular Polygon envelope from viewport bounding box coordinates.
    /// Optimized for map viewport queries - creates a proper PostGIS envelope.
    /// Coordinates are ordered counterclockwise: SW -> SE -> NE -> NW -> SW (closed ring).
    /// </summary>
    /// <param name="viewport">The viewport bounding box with min/max latitude and longitude</param>
    /// <returns>A rectangular Polygon representing the viewport envelope with SRID 4326</returns>
    /// <exception cref="ArgumentNullException">Thrown when viewport is null</exception>
    /// <exception cref="ArgumentException">Thrown when viewport bounds are invalid</exception>
    public static Polygon CreateEnvelopeFromViewport(ViewportBounds viewport)
    {
        if (viewport == null)
        {
            throw new ArgumentNullException(nameof(viewport), "Viewport cannot be null.");
        }

        if (!viewport.IsValid())
        {
            throw new ArgumentException(
                $"Invalid viewport bounds: MinLatitude ({viewport.MinLatitude}) must be less than MaxLatitude ({viewport.MaxLatitude}), " +
                $"and MinLongitude ({viewport.MinLongitude}) must be less than MaxLongitude ({viewport.MaxLongitude}).",
                nameof(viewport));
        }

        // Create envelope as a rectangular polygon
        // Order: Southwest -> Southeast -> Northeast -> Northwest -> Southwest (counterclockwise, closed ring)
        // PostGIS convention: X = Longitude, Y = Latitude
        var coordinates = new[]
        {
            new Coordinate(viewport.MinLongitude, viewport.MinLatitude), // Southwest corner
            new Coordinate(viewport.MaxLongitude, viewport.MinLatitude), // Southeast corner
            new Coordinate(viewport.MaxLongitude, viewport.MaxLatitude), // Northeast corner
            new Coordinate(viewport.MinLongitude, viewport.MaxLatitude), // Northwest corner
            new Coordinate(viewport.MinLongitude, viewport.MinLatitude)  // Close the ring (back to Southwest)
        };

        var shell = new LinearRing(coordinates);
        return new Polygon(shell) { SRID = SRID };
    }

    /// <summary>
    /// Creates a rectangular Polygon envelope from explicit coordinate bounds.
    /// Alternative to CreateEnvelopeFromViewport for scenarios where ViewportBoundsDto is not used.
    /// </summary>
    /// <param name="minLatitude">Minimum latitude (southern boundary)</param>
    /// <param name="maxLatitude">Maximum latitude (northern boundary)</param>
    /// <param name="minLongitude">Minimum longitude (western boundary)</param>
    /// <param name="maxLongitude">Maximum longitude (eastern boundary)</param>
    /// <returns>A rectangular Polygon representing the envelope with SRID 4326</returns>
    /// <exception cref="ArgumentException">Thrown when coordinate bounds are invalid</exception>
    public static Polygon CreateEnvelope(double minLatitude, double maxLatitude, double minLongitude, double maxLongitude)
    {
        if (minLatitude >= maxLatitude)
        {
            throw new ArgumentException($"MinLatitude ({minLatitude}) must be less than MaxLatitude ({maxLatitude}).");
        }

        if (minLongitude >= maxLongitude)
        {
            throw new ArgumentException($"MinLongitude ({minLongitude}) must be less than MaxLongitude ({maxLongitude}).");
        }

        var coordinates = new[]
        {
            new Coordinate(minLongitude, minLatitude), // Southwest
            new Coordinate(maxLongitude, minLatitude), // Southeast
            new Coordinate(maxLongitude, maxLatitude), // Northeast
            new Coordinate(minLongitude, maxLatitude), // Northwest
            new Coordinate(minLongitude, minLatitude)  // Close the ring
        };

        var shell = new LinearRing(coordinates);
        return new Polygon(shell) { SRID = SRID };
    }
}