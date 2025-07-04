using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Entities;

public class CellularCoverage : BaseEntity, IGeoLocatedEntity
{
    public Guid NetworkId { get; set; }
    public virtual Network Network { get; set; } = null!;
    public string? Address { get; set; }
    public decimal Strength { get; set; }
    public Point Location { get; set; } = null!;
    public bool IsFileUploaded { get; set; }
}
