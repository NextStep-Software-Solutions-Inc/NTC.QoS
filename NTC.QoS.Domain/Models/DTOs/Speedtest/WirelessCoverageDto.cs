namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class WirelessCoverageDto
{
    public double? MinLatitude { get; set; }
    public double? MaxLatitude { get; set; }
    public double? MinLongitude { get; set; }
    public double? MaxLongitude { get; set; }

    public Guid? ServiceProviderId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
