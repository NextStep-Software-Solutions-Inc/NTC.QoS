namespace NTC.QoS.Domain.Entities;

public class Region : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<Province> Provinces { get; set; } = new();

}


