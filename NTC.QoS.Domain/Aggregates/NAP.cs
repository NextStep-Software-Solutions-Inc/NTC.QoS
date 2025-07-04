using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Aggregates;

public class NAP : BaseEntity, IGeoLocatedEntity
{
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider ServiceProvider { get; set; } = null!;
    public string Address { get; set; } = string.Empty;
    public bool IsFileUploaded { get; set; } = false;
    public Point Location { get; set; } = null!;
}
