namespace NTC.QoS.Domain.Models.DTOs.MIMO;
public class UpdateMIMODto
{
    public string Type { get; set; } = string.Empty;
    public decimal DLCoverageGain { get; set; }
    public decimal ULCoverageGain { get; set; }
    public decimal DLThroughput { get; set; }
    public decimal ULThroughput { get; set; }
    public decimal DLInterferenceReduction { get; set; }
    public decimal ULInterferenceReduction { get; set; }
}
