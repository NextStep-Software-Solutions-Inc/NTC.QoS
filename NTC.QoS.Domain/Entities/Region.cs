using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations.Schema;

namespace NTC.QoS.Domain.Entities;

public class Region : BaseEntity
{
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

    public virtual List<Province> Provinces { get; set; } = [];
}
