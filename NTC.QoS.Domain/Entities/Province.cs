namespace NTC.QoS.Domain.Entities;

public class Province : BaseEntity
{
    public Guid RegionId { get; set; }
    public virtual Region? Region { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}


