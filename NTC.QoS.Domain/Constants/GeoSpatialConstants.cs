namespace NTC.QoS.Domain.Constants;

/// <summary>
/// Geographic and spatial calculation constants for the Philippines and Asia-Pacific region.
/// </summary>
public static class GeoSpatialConstants
{
    // ============================================================================
    // COORDINATE BOUNDS FOR ASIA-PACIFIC REGION
    // ============================================================================
    
    /// <summary>
    /// Minimum latitude for supported region (includes buffer south of Philippines).
    /// Covers Indonesia border areas.
    /// </summary>
    public const double MinSupportedLatitude = -5.0;
    
    /// <summary>
    /// Maximum latitude for supported region (includes buffer north of Philippines).
    /// Covers Taiwan and southern China areas.
    /// </summary>
    public const double MaxSupportedLatitude = 25.0;
    
    /// <summary>
    /// Minimum longitude for supported region (includes buffer west of Philippines).
    /// Covers Vietnam and Malaysia areas.
    /// </summary>
    public const double MinSupportedLongitude = 110.0;
    
    /// <summary>
    /// Maximum longitude for supported region (includes buffer east of Philippines).
    /// Covers Palau and western Pacific areas.
    /// </summary>
    public const double MaxSupportedLongitude = 135.0;

    // ============================================================================
    // AREA SIZE LIMITS FOR PERFORMANCE PROTECTION
    // ============================================================================
    
    /// <summary>
    /// Maximum allowed latitude difference for bounding box queries.
    /// Approximately 1,100 km north-south span.
    /// </summary>
    public const double MaxLatitudeDifference = 10.0;
    
    /// <summary>
    /// Maximum allowed longitude difference for bounding box queries.
    /// Approximately 1,300-1,600 km east-west span (varies by latitude).
    /// </summary>
    public const double MaxLongitudeDifference = 15.0;

    // ============================================================================
    // MATHEMATICAL CONSTANTS FOR COORDINATE CONVERSIONS
    // ============================================================================
    
    /// <summary>
    /// Approximate kilometers per degree of latitude.
    /// Based on WGS84 ellipsoid: 1 degree latitude ? 111.32 km.
    /// Reference: https://en.wikipedia.org/wiki/Geographic_coordinate_system
    /// Source: "The distance between lines of latitude is constant at approximately 111.32 km (69.17 miles)"
    /// </summary>
    public const double KilometersPerDegreeLatitude = 111.32;
    
    /// <summary>
    /// Conversion factor from degrees to radians.
    /// Formula: radians = degrees × (? / 180)
    /// Reference: https://en.wikipedia.org/wiki/Radian
    /// </summary>
    public const double DegreesToRadians = Math.PI / 180.0;
    
    /// <summary>
    /// Conversion factor from radians to degrees.
    /// Formula: degrees = radians × (180 / ?)
    /// </summary>
    public const double RadiansToDegrees = 180.0 / Math.PI;

    // ============================================================================
    // VALIDATION MESSAGES
    // ============================================================================
    
    /// <summary>
    /// Error message for coordinates outside supported region.
    /// </summary>
    public static readonly string CoordinateRangeErrorMessage = 
        $"Coordinates must be within the Asia-Pacific region. " +
        $"Latitude: {MinSupportedLatitude} to {MaxSupportedLatitude}, " +
        $"Longitude: {MinSupportedLongitude} to {MaxSupportedLongitude}";
    
    /// <summary>
    /// Gets the error message for area size exceeding limits.
    /// </summary>
    /// <param name="actualLatDiff">Actual latitude difference</param>
    /// <param name="actualLonDiff">Actual longitude difference</param>
    /// <returns>Formatted error message</returns>
    public static string GetAreaSizeErrorMessage(double actualLatDiff, double actualLonDiff) =>
        $"Maximum area size: {MaxLatitudeDifference}° latitude × {MaxLongitudeDifference}° longitude. " +
        $"Current: {actualLatDiff:F2}° × {actualLonDiff:F2}°";

    // ============================================================================
    // UTILITY METHODS
    // ============================================================================
    
    /// <summary>
    /// Calculates the approximate distance in kilometers for one degree of longitude at a given latitude.
    /// At the equator, 1 degree longitude ? 111.32 km. This decreases as you move toward the poles.
    /// Formula: km_per_degree_lon = 111.32 × cos(latitude_in_radians)
    /// </summary>
    /// <param name="latitudeInDegrees">Latitude in degrees</param>
    /// <returns>Kilometers per degree of longitude at the specified latitude</returns>
    public static double GetKilometersPerDegreeLongitude(double latitudeInDegrees)
    {
        return KilometersPerDegreeLatitude * Math.Cos(latitudeInDegrees * DegreesToRadians);
    }
    
    /// <summary>
    /// Validates if coordinates are within the supported region bounds.
    /// </summary>
    /// <param name="latitude">Latitude to validate</param>
    /// <param name="longitude">Longitude to validate</param>
    /// <returns>True if coordinates are within supported bounds</returns>
    public static bool IsWithinSupportedRegion(double latitude, double longitude)
    {
        return latitude >= MinSupportedLatitude && latitude <= MaxSupportedLatitude &&
               longitude >= MinSupportedLongitude && longitude <= MaxSupportedLongitude;
    }
}