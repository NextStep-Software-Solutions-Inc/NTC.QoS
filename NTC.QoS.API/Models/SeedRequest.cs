namespace NTC.QoS.API.Models;

/// <summary>
/// Request model for selective entity reseeding
/// </summary>
public class SeedRequest
{
    /// <summary>
    /// If true, reseed Region entities
    /// </summary>
    public bool RegionEntity { get; set; }

    /// <summary>
    /// If true, reseed Province entities
    /// </summary>
    public bool ProvinceEntity { get; set; }

    /// <summary>
    /// If true, reseed Municipality entities
    /// </summary>
    public bool MunicipalityEntity { get; set; }

    /// <summary>
    /// If true, reseed Barangay entities
    /// </summary>
    public bool BarangayEntity { get; set; }

    /// <summary>
    /// If true, forces reseeding even if data exists (updates existing records)
    /// </summary>
    public bool ForceReseed { get; set; }
}
