namespace NTC.QoS.Domain.Models.DTOs.Dashboard;

public class TopProvincesInRegionByCellCountDto
{
    public Guid RegionId { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int CellCount { get; set; }
}
