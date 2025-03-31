namespace NTC.QoS.Domain.Entities;

public class MIMO : BaseEntity
{
    public string Type { get; set; } = string.Empty;
    public decimal DLCoverageGain { get; set; }
    public decimal ULCoverageGain { get; set; }
    public decimal DLThroughput { get; set; }
    public decimal ULThroughput { get; set; }
    public decimal DLInterferenceReduction { get; set; }
    public decimal ULInterferenceReduction { get; set; }
    public virtual List<Sector> Sectors { get; set; } = new();
}
