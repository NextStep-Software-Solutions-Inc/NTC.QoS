
using NTC.QoS.Domain.Entities;
using NetTopologySuite.Geometries;

namespace NTC.QoS.Domain.Aggregates;

public class NAP : BaseEntity
{
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider ServiceProvider { get; set; } = null!;
    public string Address { get; set; } = string.Empty;
    public bool IsFileUploaded { get; set; } = false;
    public Point Location { get; set; } = null!;
}
