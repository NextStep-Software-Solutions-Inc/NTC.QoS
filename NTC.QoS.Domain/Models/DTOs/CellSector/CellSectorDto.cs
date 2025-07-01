using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Domain.Models.DTOs.CellSector;

public class CellSectorDto
{
    public required Guid Id { get; set; }
    public CellDto? Cell { get; set; }
    public string? AntennaType { get; set; }
    public decimal AntennaHeight { get; set; }
    public decimal Azimuth { get; set; }
    public decimal BeamTilt { get; set; }
}
