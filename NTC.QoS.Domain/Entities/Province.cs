using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTC.QoS.Domain.Entities;

public class Province : BaseEntity
{
    public Guid RegionId { get; set; }
    public virtual Region Region { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MultiPolygon Boundaries { get; set; } = null!;
    public Point Center { get; set; } = null!;
    public string Code { get; set; } = null!;
    public float Area { get; set; } = 0f;

    [NotMapped]
    public MultiPolygon BoundariesGeometry { get; set; } = null!;
    [NotMapped]
    public Point CenterGeometry { get; set; } = null!;
}
