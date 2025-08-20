using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.Generation;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Technology;
using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class SpeedtestDto
{
    public Guid Id { get; set; }
    public ServiceProviderDto? ServiceProvider { get; set; }
    public GenerationDto? Generation { get; set; }
    public TechnologyDto? Technology { get; set; }
    public FileDto? File { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Ping { get; set; }
    public decimal Jitter { get; set; }
    public decimal? RSSI { get; set; }
    public decimal UploadSpeed { get; set; }
    public decimal DownloadSpeed { get; set; }
    public required string PhoneModel { get; set; }
    [JsonPropertyName("MCC")] public required string MCC { get; set; }
    [JsonPropertyName("MNC")] public required string MNC { get; set; }
    [JsonPropertyName("TAC")] public required string TAC { get; set; } 
    public string? SignalQuality { get; set; }
    public required string CellIdentifier { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Distance { get; set; }
    public string Address { get; set; } = string.Empty;
    public bool IsFileUploaded { get; set; } = false;
    public bool IsFixedBroadband { get; set; } = false;
}
