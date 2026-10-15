using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Entities;

public class Speedtest : BaseEntity, IGeoLocatedEntity
{
    public string Address { get; set; } = string.Empty;
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider ServiceProvider { get; set; } = null!;
    public Guid? GenerationId { get; set; }
    public virtual Generation? Generation { get; set; }
    public Guid? TechnologyId { get; set; }
    public virtual Technology? Technology { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Ping { get; set; }
    public decimal Jitter { get; set; }
    public decimal? RSSI { get; set; }
    public decimal UploadSpeed { get; set; }
    public decimal DownloadSpeed { get; set; }
    public Point Location { get; set; } = null!;
    public string PhoneModel { get; set; } = string.Empty;
    public string MCC { get; set; } = string.Empty;
    public string MNC { get; set; } = string.Empty;
    public string TAC { get; set; } = string.Empty;
    public string? SignalQuality { get; set; }
    public bool IsFileUploaded { get; set; }
    public string CellIdentifier { get; set; } = string.Empty;
    public bool IsFixedBroadband { get; set; }
    public bool IsNoBroadbandCoverage { get; set; }
}
