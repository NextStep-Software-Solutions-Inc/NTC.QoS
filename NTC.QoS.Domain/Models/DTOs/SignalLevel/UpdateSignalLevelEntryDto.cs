namespace NTC.QoS.Domain.Models.DTOs.SignalLevel;

public class UpdateSignalLevelEntryDto
{
    public string? Color { get; set; }
    public decimal? ThresholdFrom { get; set; }
    public decimal? ThresholdTo { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
}