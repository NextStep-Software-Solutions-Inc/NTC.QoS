namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class GetWirelessCoverageByTechnologyRequest
{
    public double? MinLatitude { get; set; }
    public double? MaxLatitude { get; set; }
    public double? MinLongitude { get; set; }
    public double? MaxLongitude { get; set; }

    public Guid? ServiceProviderId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
