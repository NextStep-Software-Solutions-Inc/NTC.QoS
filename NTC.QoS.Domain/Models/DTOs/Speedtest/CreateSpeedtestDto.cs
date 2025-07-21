using System.Transactions;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class CreateSpeedtestDto
{
    public string Address { get; set; } = string.Empty;
    public Guid ServiceProviderId { get; set; }
    public Guid GenerationId { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Ping { get; set; }
    public decimal Jitter { get; set; }
    public decimal RSSI { get; set; }
    public decimal UploadSpeed { get; set; }
    public decimal DownloadSpeed { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; } 
    public required string PhoneModel { get; set; } 
    public required string MCC { get; set; } 
    public required string MNC { get; set; } 
    public required string TAC { get; set; }
    public SignalQualityEnum SignalQuality { get; set; } = SignalQualityEnum.Unknown;
    public required string CellIdentifier { get; set; }
}
