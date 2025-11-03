using System.ComponentModel.DataAnnotations;
using NTC.QoS.Domain.Constants;

namespace NTC.QoS.Domain.Models.DTOs.Common;

/// <summary>
/// Represents a rectangular viewport bounding box for map-based spatial queries.
/// Optimized for UI map viewport filtering to reduce data transfer and prevent OOM.
/// Includes validation for limited resource environments handling millions of data points.
/// </summary>
public class ViewportBounds
{
    /// <summary>
    /// Minimum latitude (southern boundary) of the viewport.
    /// Must be between -90 and 90 degrees, and less than MaxLatitude.
    /// </summary>
    [Required]
    [Range(-90, 90, ErrorMessage = "MinLatitude must be between -90 and 90 degrees")]
    public double MinLatitude { get; set; }

    /// <summary>
    /// Maximum latitude (northern boundary) of the viewport.
    /// Must be between -90 and 90 degrees, and greater than MinLatitude.
    /// </summary>
    [Required]
    [Range(-90, 90, ErrorMessage = "MaxLatitude must be between -90 and 90 degrees")]
    public double MaxLatitude { get; set; }

    /// <summary>
    /// Minimum longitude (western boundary) of the viewport.
    /// Must be between -180 and 180 degrees, and less than MaxLongitude.
    /// </summary>
    [Required]
    [Range(-180, 180, ErrorMessage = "MinLongitude must be between -180 and 180 degrees")]
    public double MinLongitude { get; set; }

    /// <summary>
    /// Maximum longitude (eastern boundary) of the viewport.
    /// Must be between -180 and 180 degrees, and greater than MinLongitude.
    /// </summary>
    [Required]
    [Range(-180, 180, ErrorMessage = "MaxLongitude must be between -180 and 180 degrees")]
    public double MaxLongitude { get; set; }

    /// <summary>
    /// Validates that the viewport bounds are logically correct.
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return MinLatitude < MaxLatitude && MinLongitude < MaxLongitude;
    }

    /// <summary>
    /// Validates viewport with resource-aware limits.
    /// Checks area size to prevent excessive queries on limited infrastructure.
    /// </summary>
    /// <param name="errorMessage">Output error message if validation fails</param>
    /// <returns>True if viewport is within acceptable resource limits</returns>
    public bool IsValidForLimitedResources(out string? errorMessage)
    {
        errorMessage = null;

        if (!IsValid())
        {
            errorMessage = "Invalid viewport bounds: MinLatitude must be < MaxLatitude and MinLongitude must be < MaxLongitude.";
            return false;
        }

        var area = GetAreaInSquareDegrees();
        if (area > GeoSpatialConstants.MaxViewportAreaSquareDegrees)
        {
            errorMessage = $"Viewport area too large ({area:F2} sq degrees). Maximum allowed: {GeoSpatialConstants.MaxViewportAreaSquareDegrees} sq degrees. " +
                          $"Please zoom in or reduce the viewport size to prevent server resource exhaustion.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the approximate area of the viewport in square degrees.
    /// Useful for determining viewport size for optimization decisions.
    /// </summary>
    /// <returns>Area in square degrees</returns>
    public double GetAreaInSquareDegrees()
    {
        return (MaxLatitude - MinLatitude) * (MaxLongitude - MinLongitude);
    }

    /// <summary>
    /// Determines the viewport size category based on area.
    /// Used to apply different pagination limits based on viewport zoom level.
    /// </summary>
    /// <returns>Viewport size category</returns>
    public ViewportSizeCategory GetSizeCategory()
    {
        var area = GetAreaInSquareDegrees();
        
        if (area <= GeoSpatialConstants.SmallViewportThresholdSquareDegrees)
            return ViewportSizeCategory.Small;
        
        if (area <= GeoSpatialConstants.MediumViewportThresholdSquareDegrees)
            return ViewportSizeCategory.Medium;
        
        return ViewportSizeCategory.Large;
    }

    /// <summary>
    /// Gets the recommended maximum results for this viewport size.
    /// Enforces resource limits based on viewport zoom level.
    /// </summary>
    /// <returns>Maximum recommended results</returns>
    public int GetRecommendedMaxResults()
    {
        return GetSizeCategory() switch
        {
            ViewportSizeCategory.Small => GeoSpatialConstants.MaxViewportPageSize,
            ViewportSizeCategory.Medium => GeoSpatialConstants.MaxResultsForMediumViewport,
            ViewportSizeCategory.Large => GeoSpatialConstants.MaxResultsForLargeViewport,
            _ => GeoSpatialConstants.MaxResultsForLargeViewport
        };
    }

    /// <summary>
    /// Gets the center point of the viewport.
    /// </summary>
    /// <returns>Tuple of (Latitude, Longitude)</returns>
    public (double Latitude, double Longitude) GetCenter()
    {
        return (
            (MinLatitude + MaxLatitude) / 2,
            (MinLongitude + MaxLongitude) / 2
        );
    }

    /// <summary>
    /// Generates a cache-friendly string representation of the viewport.
    /// Useful for cache key generation.
    /// </summary>
    /// <returns>String representation suitable for cache keys</returns>
    public string ToCacheKey()
    {
        return $"{MinLatitude:F6}_{MaxLatitude:F6}_{MinLongitude:F6}_{MaxLongitude:F6}";
    }

    public override string ToString()
    {
        return $"Viewport[({MinLatitude}, {MinLongitude}) to ({MaxLatitude}, {MaxLongitude})]";
    }
}

/// <summary>
/// Categorizes viewport size for resource-aware query optimization.
/// Larger viewports require more aggressive pagination limits.
/// </summary>
public enum ViewportSizeCategory
{
    /// <summary>
    /// Small viewport (zoomed in) - can return more results
    /// Area ≤ 1 square degree (~110km × 111km)
    /// </summary>
    Small = 1,

    /// <summary>
    /// Medium viewport - moderate result limits
    /// Area ≤ 10 square degrees (~330km × 333km)
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Large viewport (zoomed out) - strict result limits to protect resources
    /// Area > 10 square degrees
    /// </summary>
    Large = 3
}
