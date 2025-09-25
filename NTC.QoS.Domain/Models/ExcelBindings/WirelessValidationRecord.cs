using NTC.QoS.Domain.Attributes;

namespace NTC.QoS.Domain.Models.ExcelBindings;

public class WirelessValidationRecord
{
    // Municipality info parsed from Cell A1
    public string Municipality { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public Guid? MunicipalityId { get; set; }

    // Row data fields
    public string Address { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;

    // Service provider and barangay IDs resolved during processing
    public Guid? ServiceProviderId { get; set; }
    public Guid? BarangayId { get; set; }

    [ExcelHeader("B8-900")]
    public string Gsm900 { get; set; } = string.Empty;

    [ExcelHeader("B3-1800")]
    public string Gsm1800 { get; set; } = string.Empty;

    [ExcelHeader("B5-850")]
    public string Wcdma850 { get; set; } = string.Empty;

    [ExcelHeader("B8-900")]
    public string Wcdma900 { get; set; } = string.Empty;

    [ExcelHeader("B1-2100")]
    public string Wcdma2100 { get; set; } = string.Empty;

    [ExcelHeader("B28-700")]
    public string FddLte700 { get; set; } = string.Empty;

    [ExcelHeader("B5-850")]
    public string FddLte850 { get; set; } = string.Empty;

    [ExcelHeader("B8-900")]
    public string FddLte900 { get; set; } = string.Empty;

    [ExcelHeader("B3-1800")]
    public string FddLte1800 { get; set; } = string.Empty;

    [ExcelHeader("B1-2100")]
    public string FddLte2100 { get; set; } = string.Empty;

    [ExcelHeader("B40-2300")]
    public string TddLte2300 { get; set; } = string.Empty;

    [ExcelHeader("B41-2500")]
    public string TddLte2500 { get; set; } = string.Empty;

    [ExcelHeader("B38-2600")]
    public string TddLte2600 { get; set; } = string.Empty;

    [ExcelHeader("B28-700")]
    public string Nr700 { get; set; } = string.Empty;

    [ExcelHeader("B3-1800")]
    public string Nr1800 { get; set; } = string.Empty;

    [ExcelHeader("B1-2100")]
    public string Nr2100 { get; set; } = string.Empty;

    [ExcelHeader("B40-2300")]
    public string Nr2300 { get; set; } = string.Empty;

    [ExcelHeader("B41-2500")]
    public string Nr2500 { get; set; } = string.Empty;

    [ExcelHeader("B38-2600")]
    public string Nr2600 { get; set; } = string.Empty;

    [ExcelHeader("B78-3500")]
    public string Nr3500 { get; set; } = string.Empty;
}
