using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// CellularCoverage - Excel-based coverage point mapping
/// Enhanced to align with V2 schema while maintaining Excel file support
/// </summary>
public class CellularCoverage : BaseEntity, IGeoLocatedEntity
{
    /// <summary>
    /// Network identifier (from Excel)
    /// </summary>
    public Guid NetworkId { get; set; }
    public virtual Network Network { get; set; } = null!;

    /// <summary>
    /// Service Provider identifier (from Excel - NEW)
    /// </summary>
    public Guid? ServiceProviderId { get; set; }
    public virtual ServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// Band identifier (derived from Network - NEW)
    /// </summary>
    public Guid? BandId { get; set; }
    public virtual Band? Band { get; set; }

    /// <summary>
    /// Technology identifier (derived from Network.SystemType - NEW)
    /// </summary>
    public Guid? TechnologyId { get; set; }
    public virtual Technology? Technology { get; set; }

    /// <summary>
    /// Nearest CellSector within municipality (mapped during processing - NEW)
    /// </summary>
    public Guid? CellSectorId { get; set; }
    public virtual CellSector? CellSector { get; set; }

    /// <summary>
    /// Municipality scope (derived from location - NEW)
    /// </summary>
    public Guid? MunicipalityId { get; set; }
    public virtual Municipality? Municipality { get; set; }

    /// <summary>
    /// Geographic location of the coverage measurement
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// Signal strength measurement (from Excel)
    /// </summary>
    public decimal SignalStrength { get; set; }

    /// <summary>
    /// Address description (from Excel)
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Indicates if this record was created via file upload
    /// </summary>
    public bool IsFileUploaded { get; set; }
}
