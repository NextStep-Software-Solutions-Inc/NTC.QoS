using System.Transactions;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

public class CreateSpeedtestDto
{
    public string Address { get; set; } = string.Empty;
    public required Guid ServiceProviderId { get; set; }
    public required Guid GenerationId { get; set; }
    public required Guid TechnologyId { get; set; }
    public required DateTime DateTime { get; set; }
    public decimal? Ping { get; set; }
    public decimal? Jitter { get; set; }
    public decimal? RSSI { get; set; }
    public required decimal UploadSpeed { get; set; }
    public required decimal DownloadSpeed { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public string? PhoneModel { get; set; }
    public string? MCC { get; set; }
    public string? MNC { get; set; }
    public string? TAC { get; set; }
    public string? SignalQuality { get; set; }
    public string? CellIdentifier { get; set; }
    public bool IsFixedBroadband { get; set; } = false;
}
