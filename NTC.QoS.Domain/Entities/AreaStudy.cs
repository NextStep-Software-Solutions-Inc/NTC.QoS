namespace NTC.QoS.Domain.Entities;


public class AreaStudy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<Study> Studies { get; set; } = new();
}
