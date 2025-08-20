using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Entities;

public class Region : BaseEntity, IGeoBoundaryEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MultiPolygon? Boundaries { get; set; }
    public Point Center { get; set; } = null!;
    public string Code { get; set; } = null!;
    public float Area { get; set; } = 0f;

    public virtual List<Province> Provinces { get; set; } = [];
    public virtual List<UserRegion> UserRegions { get; set; } = [];
}
