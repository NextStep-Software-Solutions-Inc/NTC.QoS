using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Entities;

public class Province : BaseEntity, IGeoBoundaryEntity
{
    public Guid RegionId { get; set; }
    public virtual Region Region { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MultiPolygon? Boundaries { get; set; }
    public Point Center { get; set; } = null!;
    public string Code { get; set; } = null!;
    public float Area { get; set; } = 0f;

    public virtual List<Municipality> Municipalities { get; set; } = [];
}
