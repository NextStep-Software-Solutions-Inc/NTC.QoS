namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Tracks broadband coverage percentages for specific Barangays.
/// Only Barangays with a BarangayCoverage entry are considered "in scope" for reporting.
/// </summary>
public class BarangayCoverage : BaseEntity
{
    /// <summary>
    /// Foreign key to the Barangay entity
    /// </summary>
    public Guid BarangayId { get; set; }
    
    /// <summary>
    /// Navigation property to the Barangay entity
    /// </summary>
    public virtual Barangay Barangay { get; set; } = null!;
    
    /// <summary>
    /// Wired FTTH coverage percentage (0-100)
    /// </summary>
    public decimal WiredFtthPercentage { get; set; }
    
    /// <summary>
    /// Wireless broadband coverage percentage (0-100)
    /// </summary>
    public decimal WirelessPercentage { get; set; }
}
