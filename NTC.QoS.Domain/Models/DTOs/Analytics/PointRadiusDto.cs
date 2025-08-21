namespace NTC.QoS.Domain.Models.DTOs.Analytics;

public class PointRadiusDto
{
    /// <summary>
    /// Latitude coordinate of the center point (Y coordinate)
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// Longitude coordinate of the center point (X coordinate)
    /// </summary>
    public double Longitude { get; set; }
    
    /// <summary>
    /// Radius in meters from the center point
    /// </summary>
    public double RadiusMeters { get; set; }
}