namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;

public class WirelessBroadbandReportResult
{
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
