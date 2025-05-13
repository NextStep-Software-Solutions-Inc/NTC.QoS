using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Entities;

public class Speedtest : BaseEntity
{
    public string Address { get; set; } = string.Empty;
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider ServiceProvider { get; set; } = null!;
    public Guid GenerationId { get; set; }
    public virtual Generation Generation { get; set; } = null!;
    public DateTime DateTime { get; set; }
    public decimal Ping { get; set; }
    public decimal Jitter { get; set; }
    public decimal RSSI { get; set; }
    public decimal UploadSpeed { get; set; }
    public decimal DownloadSpeed { get; set; }
    public Point Location { get; set; } = null!;
    public string PhoneModel { get; set; } = string.Empty;
    public string MCC { get; set; } = string.Empty;
    public string MNC { get; set; } = string.Empty;
    public string TAC { get; set; } = string.Empty;
    public SignalQualityEnum SignalQuality { get; set; } = SignalQualityEnum.Unknown;
    public bool IsFileUploaded { get; set; }
    public string CellIdentifier { get; set; } = string.Empty;
}
