using NTC.QoS.Domain.Attributes;

namespace NTC.QoS.Domain.Models.ExcelBindings;
public record SpeedtestRecord
{
    [ExcelHeader("Region")]
    public string Region { get; set; } = string.Empty;

    [ExcelHeader("Province")]
    public string Province { get; set; } = string.Empty;

    [ExcelHeader("City")]
    public string City { get; set; } = string.Empty;

    [ExcelHeader("Barangay")]
    public string Barangay { get; set; } = string.Empty;

    [ExcelHeader("Timestamp")]
    public DateTime Timestamp { get; set; }

    [ExcelHeader("Generation")]
    public string Generation { get; set; } = string.Empty;

    [ExcelHeader("Provider")]
    public string Provider { get; set; } = string.Empty;

    [ExcelHeader("Upload")]
    public decimal Upload { get; set; }

    [ExcelHeader("Download")]
    public decimal Download { get; set; }

    [ExcelHeader("RSSI")]
    public string RSSI { get; set; } = string.Empty;

    [ExcelHeader("Remarks")]
    public string Remarks { get; set; } = string.Empty;
}