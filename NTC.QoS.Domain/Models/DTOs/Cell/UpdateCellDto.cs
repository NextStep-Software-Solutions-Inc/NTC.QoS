namespace NTC.QoS.Domain.Models.DTOs.Cell;

public class UpdateCellDto
{
    public string? CID { get; set; }
    public string? LAC { get; set; }
    public string? TAC { get; set; }
    public string? SiteId { get; set; }
    public string? TowerType { get; set; }
    public float Elevation { get; set; }
    public bool? IsVerified { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
