using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class CellSectorDto
{
    public required Guid Id { get; set; }
    public required CellDto Cell { get; set; }
    public required string AntennaType { get; set; }
    public required decimal Azimuth { get; set; }
    public required decimal BeamTilt { get; set; }
}
