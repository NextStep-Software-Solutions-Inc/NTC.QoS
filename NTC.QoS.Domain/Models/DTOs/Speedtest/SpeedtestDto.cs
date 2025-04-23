using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.Generation;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class SpeedtestDto
{
    public Guid Id { get; set; }
    public required BarangayDto Barangay { get; set; }
    public ServiceProviderDto? ServiceProvider { get; set; }
    public GenerationDto? Generation { get; set; }
    public required CellDto Cell { get; set; }
    public FileDto? File { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Ping { get; set; }
    public decimal Jitter { get; set; }
    public decimal RSSI { get; set; }
    public decimal UploadSpeed { get; set; }
    public decimal DownloadSpeed { get; set; }
    public required string CoorLatitude { get; set; } 
    public required string CoorLongitude { get; set; } 
    public required string PhoneModel { get; set; }
    [JsonPropertyName("MCC")] public required string MCC { get; set; }
    [JsonPropertyName("MNC")] public required string MNC { get; set; }
    [JsonPropertyName("TAC")] public required string TAC { get; set; } 
    public SignalQualityEnum SignalQuality { get; set; } = SignalQualityEnum.Unknown;
}
