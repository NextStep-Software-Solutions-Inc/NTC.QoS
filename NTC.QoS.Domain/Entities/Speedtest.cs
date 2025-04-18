using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Entities;

public class Speedtest : BaseEntity
{
    public Guid BarangayId { get; set; }
    public virtual Barangay Barangay { get; set; } = null!;
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider ServiceProvider { get; set; } = null!;
    public Guid GenerationId { get; set; }
    public virtual Generation Generation { get; set; } = null!;
    public Guid CellId { get; set; }
    public virtual Cell Cell { get; set; } = null!;
    public Guid? FileId { get; set; }
    public virtual File? File { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Ping { get; set; }
    public decimal Jitter { get; set; }
    public decimal RSSI { get; set; }
    public decimal UploadSpeed { get; set; }
    public decimal DownloadSpeed { get; set; }
    public string CoorLatitude { get; set; } = string.Empty;
    public string CoorLongitude { get; set; } = string.Empty;
    public string PhoneModel { get; set; } = string.Empty;
    public string MCC { get; set; } = string.Empty;
    public string MNC { get; set; } = string.Empty;
    public string TAC { get; set; } = string.Empty;
    public SignalQualityEnum SignalQuality { get; set; } = SignalQualityEnum.Unknown;

}


