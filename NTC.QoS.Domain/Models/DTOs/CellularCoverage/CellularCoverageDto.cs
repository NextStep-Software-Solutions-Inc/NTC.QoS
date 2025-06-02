namespace NTC.QoS.Domain.Models.DTOs.CellularCoverage;

public class CellularCoverageDto
{
    public Guid Id { get; set; }
    public Guid NetworkId { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public decimal Strength { get; set; }
    public string Gen { get; set; } = string.Empty;
    public string Std { get; set; } = string.Empty;
    public string Band { get; set; } = string.Empty;
    public virtual CCProviderDto? Provider { get; set; }
}

public class CCProviderDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
