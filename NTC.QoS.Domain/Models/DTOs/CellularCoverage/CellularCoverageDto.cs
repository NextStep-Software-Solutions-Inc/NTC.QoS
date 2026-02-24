namespace NTC.QoS.Domain.Models.DTOs.CellularCoverage;

public class CellularCoverageDto
{
    public Guid Id { get; set; }
    public Guid NetworkId { get; set; }
    public string NetworkName { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public decimal SignalStrength { get; set; }
    public string Gen { get; set; } = string.Empty;
    public string Std { get; set; } = string.Empty;
    public string Band { get; set; } = string.Empty;
    public virtual CCProviderDto? Provider { get; set; }
    
    // V2 Extended Properties (nullable for backward compatibility)
    public Guid? BandId { get; set; }
    public string? BandName { get; set; }
    public Guid? TechnologyId { get; set; }
    public string? TechnologyName { get; set; }
    public Guid? CellSectorId { get; set; }
    public string? CellSectorName { get; set; }
    public Guid? MunicipalityId { get; set; }
    public string? MunicipalityName { get; set; }
    public string? Address { get; set; }
    public bool IsFileUploaded { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CCProviderDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
