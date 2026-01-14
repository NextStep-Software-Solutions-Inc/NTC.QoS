namespace NTC.QoS.Domain.Models.DTOs;

/// <summary>
/// Parameters for location seeding background job
/// </summary>
public class LocationSeedParametersDto
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

    /// <summary>
    /// If true, use the optimized LocationSeederV2
    /// </summary>
    public bool UseOptimizedSeeder { get; set; } = true;
}
