using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Entities;

public class Barangay : BaseEntity
{
    public Guid MunicipalityId { get; set; }
    public virtual Municipality Municipality { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MultiPolygon Boundaries { get; set; } = null!;
    public Point Center { get; set; } = null!;
    public string Code { get; set; } = null!;
    public float Area { get; set; } = 0f;

    public virtual List<Speedtest> Speedtests { get; set; } = new();
    public virtual List<Cellular> Cellulars { get; set; } = new();
    public virtual List<Zone> Zones { get; set; } = new();

}
