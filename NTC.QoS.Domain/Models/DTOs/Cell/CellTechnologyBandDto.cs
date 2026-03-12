namespace NTC.QoS.Domain.Models.DTOs.Cell;

public class CellTechnologyBandDto
{
    public Guid TechnologyId { get; set; }
    public string TechnologyStandard { get; set; } = string.Empty;
    public string? TechnologyDescription { get; set; }
    public List<CellBandItemDto> Bands { get; set; } = [];
}

public class CellBandItemDto
{
    public Guid BandId { get; set; }
    public string BandName { get; set; } = string.Empty;
    public string? BandDescription { get; set; }
}
