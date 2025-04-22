using NTC.QoS.Domain.Attributes;

namespace NTC.QoS.Domain.Models.ExcelBindings;
public record NAPRecord
{
    [ExcelHeader("Provider")]
    public string Provider { get; set; } = string.Empty;

    [ExcelHeader("Municipality")]
    public string Municipality { get; set; } = string.Empty;

    [ExcelHeader("Province")]
    public string Province { get; set; } = string.Empty;

    [ExcelHeader("Lng")]
    public double Latitude { get; set; }

    [ExcelHeader("Lat")]
    public double Longitude { get; set; }
}