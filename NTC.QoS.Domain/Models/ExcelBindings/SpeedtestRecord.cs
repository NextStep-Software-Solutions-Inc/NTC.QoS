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

    [ExcelHeader("ServiceProvider")]
    public string ServiceProvider { get; set; } = string.Empty;

    [ExcelHeader("Upload")]
    public decimal Upload { get; set; }

    [ExcelHeader("Download")]
    public decimal Download { get; set; }

    [ExcelHeader("SignalStrength")]
    public string SignalStrength { get; set; } = string.Empty;

    [ExcelHeader("Remarks")]
    public string Remarks { get; set; } = string.Empty;
}