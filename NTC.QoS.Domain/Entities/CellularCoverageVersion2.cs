using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Entities;

/// <summary>
/// CellularCoverage Version 2 - CSV-based coverage point mapping
/// Maps coverage measurements to infrastructure (CellSector) within municipalities
/// </summary>
public class CellularCoverageVersion2 : BaseEntity, IGeoLocatedEntity
{
    /// <summary>
    /// Network identifier (user-provided during upload)
    /// </summary>=
    public Guid NetworkId { get; set; }
    public virtual Network Network { get; set; } = null!;

    /// <summary>
    /// Band identifier (user-provided during upload)
    /// </summary>
    public Guid BandId { get; set; }
    public virtual Band Band { get; set; } = null!;

    /// <summary>
    /// Technology identifier (derived from Network.SystemType)
    /// </summary>
    public Guid TechnologyId { get; set; }
    public virtual Technology Technology { get; set; } = null!;

    /// <summary>
    /// Nearest CellSector within the same municipality (mapped during processing)
    /// </summary>
    public Guid CellSectorId { get; set; }
    public virtual CellSector CellSector { get; set; } = null!;

    /// <summary>
    /// Municipality scope (user-provided during upload)
    /// </summary>
    public Guid MunicipalityId { get; set; }
    public virtual Municipality Municipality { get; set; } = null!;

    /// <summary>
    /// Geographic location of the coverage measurement
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// Signal strength measurement (from CSV)
    /// </summary>
    public decimal SignalStrength { get; set; }

    /// <summary>
    /// Address description (from CSV)
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Indicates if this record was created via CSV upload
    /// </summary>
    public bool IsFileUploaded { get; set; }
}