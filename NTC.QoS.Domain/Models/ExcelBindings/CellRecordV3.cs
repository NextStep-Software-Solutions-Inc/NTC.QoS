using NTC.QoS.Domain.Attributes;

namespace NTC.QoS.Domain.Models.ExcelBindings;

public record CellRecordV3
{
    [ExcelHeader("Provider")]
    public string Provider { get; set; } = string.Empty;

    [ExcelHeader("Zone")]
    public string Zone { get; set; } = string.Empty;

    [ExcelHeader("Barangay")]
    public string Barangay { get; set; } = string.Empty;

    [ExcelHeader("Municipality")]
    public string Municipality { get; set; } = string.Empty;

    [ExcelHeader("Province")]
    public string Province { get; set; } = string.Empty;

    [ExcelHeader("Region")]
    public string Region { get; set; } = string.Empty;

    [ExcelHeader("Lat")]
    public double Latitude { get; set; }

    [ExcelHeader("Long")]
    public double Longitude { get; set; }

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

    //5G NR
    [ExcelHeader("B28-700")]
    public string Nr700 { get; set; } = string.Empty;

    [ExcelHeader("B5-850")]
    public string Nr850 { get; set; } = string.Empty;

    [ExcelHeader("B8-900")]
    public string Nr900 { get; set; } = string.Empty;

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

    [ExcelHeader("Tower Type")]
    public string TowerType { get; set; } = string.Empty;

    [ExcelHeader("Antenna Type")]
    public string AntennaType { get; set; } = string.Empty;

    [ExcelHeader("Antenna Height (m)")]
    public decimal AntennaHeight { get; set; }

    [ExcelHeader("Azimuth (°)")]
    public string Azimuth { get; set; } = string.Empty;

    [ExcelHeader("Beam Tilt (°)")]
    public string BeamTilt { get; set; } = string.Empty;
}