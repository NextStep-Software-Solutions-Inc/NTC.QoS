namespace NTC.QoS.Domain.Models.DTOs.CellularCoverage;

/// <summary>
/// Data Transfer Object for CellularCoverage Version 2
/// </summary>
public class CellularCoverageV2Dto
{
    public Guid Id { get; set; }
    
    public Guid NetworkId { get; set; }
    public string NetworkName { get; set; } = string.Empty;
    
    public Guid BandId { get; set; }
    public string BandName { get; set; } = string.Empty;
    
    public Guid TechnologyId { get; set; }
    public string TechnologyName { get; set; } = string.Empty;
    
    public Guid CellSectorId { get; set; }
    public string CellSectorName { get; set; } = string.Empty;
    
    public Guid MunicipalityId { get; set; }
    public string MunicipalityName { get; set; } = string.Empty;
    
    public double Lat { get; set; }
    public double Lng { get; set; }
    
    public decimal SignalStrength { get; set; }
    
    public string? Address { get; set; }
    
    public bool IsFileUploaded { get; set; }
    
    public DateTime CreatedAt { get; set; }
}