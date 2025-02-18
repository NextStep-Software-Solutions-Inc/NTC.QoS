namespace NTC.QoS.Domain.Entities;

public class AntennaPattern : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public virtual ICollection<Sector>? TxSectors { get; set; }
    public virtual ICollection<Sector>? RxSectors { get; set; }

}
