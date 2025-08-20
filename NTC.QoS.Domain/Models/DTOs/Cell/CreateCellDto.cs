namespace NTC.QoS.Domain.Models.DTOs.Cell;

public class CreateCellDto 
{
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid ServiceProviderId { get; set; }
    public string CID { get; set; } = string.Empty;
    public string? LAC { get; set; }
    public string TAC { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string TowerType { get; set; } = string.Empty;
    public float Elevation { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
}
