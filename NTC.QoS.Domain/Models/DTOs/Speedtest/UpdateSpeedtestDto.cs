using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.Generation;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class UpdateSpeedtestDto
{
    public decimal Ping { get; set; }
    public decimal Jitter { get; set; }
    public decimal RSSI { get; set; }
    public decimal UploadSpeed { get; set; }
    public decimal DownloadSpeed { get; set; }
    public required string CoorLatitude { get; set; } 
    public required string CoorLongitude { get; set; } 
    public required string PhoneModel { get; set; } 
    public required string MCC { get; set; } 
    public required string MNC { get; set; } 
    public required string TAC { get; set; } 
    public string? SignalQuality { get; set; }
}
