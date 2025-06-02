namespace NTC.QoS.Domain.Models.DTOs.CellularCoverage;

public class CreateCellularCoverageDto
{
    public required Guid NetworkId { get; set; }
    public required decimal Strength { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public string? Address { get; set; } = string.Empty;
}
