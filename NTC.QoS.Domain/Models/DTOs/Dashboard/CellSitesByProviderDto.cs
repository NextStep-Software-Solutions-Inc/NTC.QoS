namespace NTC.QoS.Domain.Models.DTOs.Dashboard;

public class CellSitesByProviderDto
{
    public Guid ServiceProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderColor { get; set; }
    public int CellCount { get; set; }
    public double Percentage { get; set; }
}

public class CellSitesByProviderResponseDto
{
    public int TotalCellSites { get; set; }
    public List<CellSitesByProviderDto> Providers { get; set; } = new();
}
