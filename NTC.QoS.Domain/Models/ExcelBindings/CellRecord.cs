using NTC.QoS.Domain.Attributes;

namespace NTC.QoS.Domain.Models.ExcelBindings;

public record CellRecord
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
    public string Latitude { get; set; } = string.Empty;

    [ExcelHeader("Long")]
    public string Longitude { get; set; } = string.Empty;

    [ExcelHeader("900")]
    public bool Gsm900 { get; set; }

    [ExcelHeader("1800")]
    public bool Gsm1800 { get; set; }

    [ExcelHeader("850")]
    public bool Wcdma850 { get; set; }

    [ExcelHeader("2100")]
    public bool Wcdma2100 { get; set; }

    [ExcelHeader("2100")]
    public bool FddLte2100 { get; set; }

    [ExcelHeader("900")]
    public bool FddLte900 { get; set; }

    [ExcelHeader("1800")]
    public bool FddLte1800 { get; set; }

    [ExcelHeader("700")]
    public bool FddLte700 { get; set; }

    [ExcelHeader("2300")]
    public bool TddLte2300 { get; set; }

    [ExcelHeader("2500")]
    public bool TddLte2500 { get; set; }

    [ExcelHeader("2600")]
    public bool TddLte2600 { get; set; }

    [ExcelHeader("2600")]
    public bool Nr2600 { get; set; }

    [ExcelHeader("3500")]
    public bool Nr3500 { get; set; }

    [ExcelHeader("Tower Type")]
    public string TowerType { get; set; } = string.Empty;

    [ExcelHeader("Antenna Type")]
    public string AntennaType { get; set; } = string.Empty;

    [ExcelHeader("Antenna Height (m)")]
    public decimal AntennaHeight { get; set; }

    [ExcelHeader("Azimuth (°)")]
    public string Azimuth { get; set; } = string.Empty;

    [ExcelHeader("Beam Tilt (°)")]
    public decimal BeamTilt { get; set; }
}