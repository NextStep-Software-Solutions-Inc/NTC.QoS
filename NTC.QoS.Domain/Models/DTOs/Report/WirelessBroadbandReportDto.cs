namespace NTC.QoS.Domain.Models.DTOs.Report;

public class WirelessBroadbandReportDto
{
    public string MunicipalityName { get; set; } = string.Empty;
    public List<BarangayWirelessEntryDto> Barangays { get; set; } = new();
}

public class BarangayWirelessEntryDto
{
    public string BarangayName { get; set; } = string.Empty;
    public ProviderWirelessStatsDto Globe { get; set; } = new();
    public ProviderWirelessStatsDto Smart { get; set; } = new();
    public ProviderWirelessStatsDto Dito { get; set; } = new();
}

public class ProviderWirelessStatsDto
{
    public bool Has2G { get; set; }
    public bool Has3G { get; set; }
    public bool Has4G { get; set; }
    public bool Has5G { get; set; }
    public string TechUsed { get; set; } = string.Empty;
    public double? UploadSpeedMbps { get; set; }
    public double? DownloadSpeedMbps { get; set; }
}
