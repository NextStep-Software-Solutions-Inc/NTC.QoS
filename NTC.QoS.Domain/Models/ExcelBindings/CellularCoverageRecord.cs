using NTC.QoS.Domain.Attributes;

namespace NTC.QoS.Domain.Models.ExcelBindings;

public record CellularCoverageRecord
{
    [ExcelHeader("ServiceProvider")]
    public string ServiceProvider { get; set; } = string.Empty;

    [ExcelHeader("Network")]
    public string Network { get; set; } = string.Empty;

    [ExcelHeader("Lat")]
    public double Latitude { get; set; }

    [ExcelHeader("Lng")]
    public double Longitude { get; set; }

    [ExcelHeader("Strength")]
    public decimal Strength { get; set; }

    [ExcelHeader("Address")]
    public string Address { get; set; } = string.Empty;
}
