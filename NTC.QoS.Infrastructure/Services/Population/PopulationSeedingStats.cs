namespace NTC.QoS.Infrastructure.Services.Population;

/// <summary>
/// Statistics tracking for population data seeding operations
/// </summary>
public class PopulationSeedingStats
{
    // Regions
    public int RegionsMatched { get; set; } = 0;
    public int RegionsNotFound { get; set; } = 0;
    public int RegionsErrors { get; set; } = 0;

    // Provinces
    public int ProvincesMatched { get; set; } = 0;
    public int ProvincesNotFound { get; set; } = 0;
    public int ProvincesRegionNotFound { get; set; } = 0;
    public int ProvincesErrors { get; set; } = 0;

    // Municipalities  
    public int MunicipalitiesMatched { get; set; } = 0;
    public int MunicipalitiesNotFound { get; set; } = 0;
    public int MunicipalitiesRegionNotFound { get; set; } = 0;
    public int MunicipalitiesErrors { get; set; } = 0;

    // Helper properties
    public bool HasRegionData => RegionsMatched > 0 || RegionsNotFound > 0 || RegionsErrors > 0;
    public bool HasProvinceData => ProvincesMatched > 0 || ProvincesNotFound > 0 || ProvincesRegionNotFound > 0 || ProvincesErrors > 0;
    public bool HasMunicipalityData => MunicipalitiesMatched > 0 || MunicipalitiesNotFound > 0 || 
                                       MunicipalitiesRegionNotFound > 0 || MunicipalitiesErrors > 0;
}