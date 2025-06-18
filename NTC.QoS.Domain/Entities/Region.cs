using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Entities;

public class Region : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MultiPolygon Boundaries { get; set; } = null!;
    public Point Center { get; set; } = null!;
    public string Code { get; set; } = null!;
    public float Area { get; set; } = 0f;

    public virtual List<Province> Provinces { get; set; } = [];
}
