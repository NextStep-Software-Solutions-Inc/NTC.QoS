namespace NTC.QoS.Domain.Models.DTOs.Dashboard;

public class TopBarangaysInMunicipalitiesByCellCountDto
{
    public Guid MunicipalityId { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int CellCount { get; set; }
}
