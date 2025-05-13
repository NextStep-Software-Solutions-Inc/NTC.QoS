using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using System.Drawing;

namespace NTC.QoS.Domain.Models.DTOs.Cell;

public class CellDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string Group { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public ServiceProviderDto? ServiceProvider { get; set; }
    public required string CID { get; set; }
    public required string? LAC { get; set; }
    public string TAC { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string TowerType { get; set; } = string.Empty;
    public float Elevation { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsVerified { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsFileUploaded { get; set; }
    public object Distance { get; set; }
}
