using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Entities;

public class Barangay : BaseEntity, IGeoBoundaryEntity
{
    public Guid MunicipalityId { get; set; }
    public virtual Municipality Municipality { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MultiPolygon? Boundaries { get; set; }
    public Point Center { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public float Area { get; set; } = 0f;

    // Census data fields aligned with available data structure
    /// <summary>
    /// Total population in the barangay (from census data)
    /// </summary>
    public int? TotalPopulation { get; set; }

    /// <summary>
    /// Household population in the barangay (from census data)
    /// Usually excludes institutional population
    /// </summary>
    public int? HouseholdPopulation { get; set; }

    /// <summary>
    /// Total number of households in the barangay (from census data)
    /// </summary>
    public int? HouseholdCount { get; set; }

    /// <summary>
    /// Population density per km² based on total population (calculated field)
    /// </summary>
    public double PopulationDensity => TotalPopulation.HasValue && Area > 0 ? TotalPopulation.Value / Area : 0;

    /// <summary>
    /// Average household size (calculated field)
    /// </summary>
    public double AverageHouseholdSize => HouseholdPopulation.HasValue && HouseholdCount.HasValue && HouseholdCount.Value > 0
        ? (double)HouseholdPopulation.Value / HouseholdCount.Value : 0;

    public virtual List<Speedtest> Speedtests { get; set; } = [];
    public virtual List<Zone> Zones { get; set; } = [];
    public virtual List<WirelessValidation> WirelessValidations { get; set; } = [];
}
