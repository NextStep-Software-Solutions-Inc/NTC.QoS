namespace NTC.QoS.Domain.Models.DTOs.Cell;

public class UpdateCellDto
{
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? FileId { get; set; }
    public Guid ServiceProviderId { get; set; }
    public Guid ZoneId { get; set; }
    public string CID { get; set; } = string.Empty;
    public string? LAC { get; set; }
    public string TAC { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string TowerType { get; set; } = string.Empty;
    public float Elevation { get; set; }
    public float CoorLatitude { get; set; }
    public float CoorLongitude { get; set; }
}
