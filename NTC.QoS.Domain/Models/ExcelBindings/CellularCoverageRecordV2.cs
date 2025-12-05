using NTC.QoS.Domain.Attributes;

namespace NTC.QoS.Domain.Models.ExcelBindings;

public record CellularCoverageRecordV2
{
    [ExcelHeader("A")]
    public double Latitude { get; set; }

    [ExcelHeader("B")]
    public double Longitude { get; set; }

    [ExcelHeader("C")]
    public decimal Strength { get; set; }

    [ExcelHeader("D")]
    public string Address { get; set; } = string.Empty;
}
