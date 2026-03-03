using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class FrequencyAllocation : BaseEntity
{
    public Guid ProviderId { get; set; }
    public virtual Provider Provider { get; set; } = null!;
    public Guid UpLinkId { get; set; }
    public virtual Link UpLink { get; set; } = null!;
    public Guid DownLinkId { get; set; }
    public virtual Link DownLink { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
