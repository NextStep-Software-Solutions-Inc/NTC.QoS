using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Cellular : BaseEntity
{
    public Guid BarangayId { get; set; }
    public virtual Barangay? Barangay { get; set; }
    public Guid CellId { get; set; }
    public virtual Cell? Cell { get; set; }
    public Guid ServiceProviderId { get; set; }
    public virtual ServiceProvider? ServiceProvider { get; set; }
    public Guid GenerationBandId { get; set; }
    public virtual GenerationBand? GenerationBand { get; set; }
    public Guid? FileId { get; set; }
    public virtual NTC.QoS.Domain.Entities.File? File { get; set; }
    public int? RowNumber { get; set; }
}
