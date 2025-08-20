namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Geographic constraints and limits for area-based queries.
/// </summary>
public class GeographicConstraintsDto
{
    public BoundingBoxDto SupportedRegion { get; set; } = new();
    public AreaSizeLimitsDto MaxAreaSize { get; set; } = new();
    public CoordinateConversionDto ConversionFactors { get; set; } = new();
}

/// <summary>
/// Maximum area size limits for queries.
/// </summary>
public class AreaSizeLimitsDto
{
    public double MaxLatitudeDifference { get; set; }
    public double MaxLongitudeDifference { get; set; }
}

/// <summary>
/// Coordinate conversion factors for geographic calculations.
/// </summary>
public class CoordinateConversionDto
{
    public double KilometersPerDegreeLatitude { get; set; }
    public double DegreesToRadians { get; set; }
}
