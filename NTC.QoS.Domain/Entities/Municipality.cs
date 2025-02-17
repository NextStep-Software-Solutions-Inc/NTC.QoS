namespace NTC.QoS.Domain.Entities;

public class Municipality : BaseEntity
{
    public Guid ProvinceId { get; set; }
    public virtual Province? Province { get; set; }
    public bool IsCity { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}


