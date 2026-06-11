namespace NTC.QoS.Domain.Models.DTOs.SignalLevel;

public class SignalLevelEntryDto
{
    public required Guid Id { get; set; }
    public required Guid SignalLevelId { get; set; }
    public required string Color { get; set; }
    public decimal? ThresholdFrom { get; set; }
    public decimal? ThresholdTo { get; set; }
    public required string Description { get; set; }
    public required int SortOrder { get; set; }
}