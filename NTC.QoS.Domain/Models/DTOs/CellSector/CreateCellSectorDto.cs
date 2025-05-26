using System.Drawing;

namespace NTC.QoS.Domain.Models.DTOs.CellSector;

public class CreateCellSectorDto
{
    public required Guid CellId { get; set; }
    public required string AntennaType { get; set; }
    public required decimal AntennaHeight { get; set; }
    public required decimal Azimuth { get; set; }
    public required decimal BeamTilt { get; set; }
}
