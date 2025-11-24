namespace NTC.QoS.Application.Features.Reports.Dashboard.DTOs;

public class DashboardReportResult
{
    public byte[] FileContent { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}
