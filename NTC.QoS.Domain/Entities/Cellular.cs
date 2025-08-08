using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Cellular : BaseEntity
{
    public Guid CellSectorId { get; set; }
    public virtual CellSector? CellSector { get; set; }
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider? ServiceProvider { get; set; }
    public Guid GenerationBandId { get; set; }
    public virtual GenerationBand? GenerationBand { get; set; }
    public Guid TechnologyId { get; set; }
    public virtual Technology? Technology { get; set; }
    public Guid BandId { get; set; }
    public virtual Band? Band { get; set; }
    public string? Address { get; set; }
    public bool IsFileUploaded { get; set; }
}
