namespace NTC.QoS.Domain.Models.DTOs.Cell;

public class CellDto
{
    public Guid Id { get; set; }
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
    public string TowerName { get; set; } = string.Empty;
    public float Elevation { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public bool IsVerified { get; set; }
    public string Status { get; set; } = string.Empty;
}
