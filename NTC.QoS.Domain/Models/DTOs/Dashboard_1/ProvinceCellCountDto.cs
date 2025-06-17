namespace NTC.QoS.Domain.Models.DTOs.Dashboard_1;

public class TopProvinceCellCountDto
{
    public Guid ProvinceId { get; set; }
    public string ProvinceName { get; set; } = string.Empty;
    public int CellCount { get; set; }
}