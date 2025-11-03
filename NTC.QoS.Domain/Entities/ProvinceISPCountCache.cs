namespace NTC.QoS.Domain.Entities;

/// <summary>
/// Cache entity to store pre-calculated ISP counts for provinces
/// This eliminates the need for expensive spatial queries on every API request
/// </summary>
public class ProvinceISPCountCache : BaseEntity
{
    /// <summary>
    /// Province ID
    /// </summary>
    public Guid ProvinceId { get; set; }
    
    /// <summary>
    /// Number of unique ISPs (service providers) operating in this province
    /// Calculated from both Cells and NAPs within province boundaries
    /// </summary>
    public int ISPCount { get; set; }
    
    /// <summary>
    /// When this cache entry was last calculated
    /// </summary>
    public DateTime LastCalculated { get; set; }
    
    /// <summary>
    /// Navigation property to Province
    /// </summary>
    public virtual Province Province { get; set; } = null!;
}
