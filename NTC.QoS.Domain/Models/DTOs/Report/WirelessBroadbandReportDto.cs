namespace NTC.QoS.Domain.Models.DTOs.Report;

public class BarangayWirelessEntryDto
{
    public string MunicipalityName { get; set; } = string.Empty;
    public string BarangayName { get; set; } = string.Empty;
    public Dictionary<string, ProviderWirelessStatsDto> ProviderStats { get; set; } = new();
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
