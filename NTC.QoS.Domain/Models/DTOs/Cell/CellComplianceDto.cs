namespace NTC.QoS.Domain.Models.DTOs.Cell;

public enum ComplianceStatus { Compliant, Warning, Unknown }

public class CellComplianceDto
{
    public Guid CellId { get; set; }
    public ComplianceStatus OverallStatus { get; set; }
    public List<CellBandComplianceItemDto> Items { get; set; } = [];
}

public class CellBandComplianceItemDto
{
    public Guid ServiceProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public Guid TechnologyId { get; set; }
    public string TechnologyStandard { get; set; } = string.Empty;
    public Guid BandId { get; set; }
    public string BandName { get; set; } = string.Empty;
    public bool IsAuthorized { get; set; }
    public string? MatchedAllocationName { get; set; }
    public string? MatchedAllocationNotes { get; set; }
}
