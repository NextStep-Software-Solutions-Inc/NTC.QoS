using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Municipality : BaseEntity
{
    public Guid ProvinceId { get; set; }
    public virtual Province Province { get; set; } = null!;
    public bool IsCity { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MultiPolygon Boundaries { get; set; } = null!;
    public Point Center { get; set; } = null!;
    public string Code { get; set; } = null!;
    public float Area { get; set; } = 0f;

    public virtual List<Barangay> Barangays { get; set; } = [];
    public virtual List<StudyMunicipality> StudyMunicipalities { get; set; } = [];
}
