namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class UpdateSpeedtestDto
{
    public string? Address { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public Guid? GenerationId { get; set; }
    public Guid? TechnologyId { get; set; }
    public DateTime? DateTime { get; set; }
    public decimal? Ping { get; set; }
    public decimal? Jitter { get; set; }
    public decimal? RSSI { get; set; }
    public decimal? UploadSpeed { get; set; }
    public decimal? DownloadSpeed { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? PhoneModel { get; set; }
    public string? MCC { get; set; }
    public string? MNC { get; set; }
    public string? TAC { get; set; }
    public string? SignalQuality { get; set; }
    public string? CellIdentifier { get; set; }
    public bool? IsFixedBroadband { get; set; }
}
