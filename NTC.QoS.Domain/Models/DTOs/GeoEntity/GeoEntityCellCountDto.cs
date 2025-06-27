namespace NTC.QoS.Domain.Models.DTOs.GeoEntity;

public class GeoEntityCellCountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int CellCount { get; set; }
    public string EntityType { get; set; } = string.Empty;
}
