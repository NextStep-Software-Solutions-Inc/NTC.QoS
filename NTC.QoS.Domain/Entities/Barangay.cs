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
    public string Code { get; set; } = null!;
    public float Area { get; set; } = 0f;

    public virtual List<Speedtest> Speedtests { get; set; } = new();
    public virtual List<Zone> Zones { get; set; } = new();
}
