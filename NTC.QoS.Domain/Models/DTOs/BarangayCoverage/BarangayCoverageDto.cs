namespace NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

public class BarangayCoverageDto
{
    public Guid Id { get; set; }
    public Guid BarangayId { get; set; }
    public string BarangayName { get; set; } = string.Empty;
    public string BarangayCode { get; set; } = string.Empty;
    public decimal WiredFtthPercentage { get; set; }
    public decimal WirelessPercentage { get; set; }
    
    /// <summary>
    /// Computed as min(WiredFtthPercentage + WirelessPercentage, 100)
    /// </summary>
    public decimal CombinedPercentage => Math.Min(WiredFtthPercentage + WirelessPercentage, 100);
    
    /// <summary>
    /// True if wired FTTH coverage is 100%
    /// </summary>
    public bool IsWiredCovered => WiredFtthPercentage == 100;
    
    /// <summary>
    /// True if wireless coverage is 100%
    /// </summary>
    public bool IsWirelessCovered => WirelessPercentage == 100;
    
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
