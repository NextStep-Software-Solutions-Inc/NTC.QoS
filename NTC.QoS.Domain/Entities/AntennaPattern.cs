namespace NTC.QoS.Domain.Entities;

public class AntennaPattern : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public virtual List<Sector> TxSectors { get; set; } = new();
    public virtual List<Sector> RxSectors { get; set; } = new();

}
