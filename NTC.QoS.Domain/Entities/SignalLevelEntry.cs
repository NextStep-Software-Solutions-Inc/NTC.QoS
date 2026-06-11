namespace NTC.QoS.Domain.Entities;

public class SignalLevelEntry : BaseEntity
{
    public Guid SignalLevelId { get; set; }
    public virtual SignalLevel? SignalLevel { get; set; }
    public string Color { get; set; } = string.Empty;
    public decimal? ThresholdFrom { get; set; }
    public decimal? ThresholdTo { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}