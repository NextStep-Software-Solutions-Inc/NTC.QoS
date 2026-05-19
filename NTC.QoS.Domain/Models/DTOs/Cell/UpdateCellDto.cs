using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.Cell;

public class UpdateCellDto
{
    public Guid? ServiceProviderId { get; set; }
    public string? Name { get; set; }
    public string? Group { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? CID { get; set; }
    public string? LAC { get; set; }
    public string? TAC { get; set; }
    public string? SiteId { get; set; }
    public string? TowerType { get; set; }
    public float? Elevation { get; set; }
    public bool? IsVerified { get; set; }
    public string? Status { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? Latitude { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double? Longitude { get; set; }
}
