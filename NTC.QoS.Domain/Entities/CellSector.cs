using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Entities;

public class CellSector : BaseEntity
{
    public Guid CellId { get; set; }
    public virtual Cell Cell { get; set; } = null!;
    public AntennaTypeEnum AntennaType { get; set; } = AntennaTypeEnum.Unknown;
    public decimal Azimuth { get; set; }
    public decimal BeamTilt { get; set; }
    public virtual List<Sector> Sectors { get; set; } = [];
}
