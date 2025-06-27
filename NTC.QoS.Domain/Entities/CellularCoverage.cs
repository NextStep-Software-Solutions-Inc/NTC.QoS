using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class CellularCoverage : BaseEntity
{
    public Guid NetworkId { get; set; }
    public virtual Network Network { get; set; } = null!;
    public string? Address { get; set; }
    public decimal Strength { get; set; }
    public Point Location { get; set; } = null!;
    public bool IsFileUploaded { get; set; }
}
