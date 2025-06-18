namespace NTC.QoS.Domain.Models.DTOs.Dashboard;

public class RegionCellCountDto
{
    public Guid RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public int CellCount { get; set; }
}
