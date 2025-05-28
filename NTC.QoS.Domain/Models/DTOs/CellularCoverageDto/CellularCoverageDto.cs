namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class CellularCoverageDto
{
    public Guid Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal Strength { get; set; }
    public string? Address { get; set; }
    public string Generation { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
    public string Band { get; set; } = string.Empty;
    public virtual CCProviderDto? Provider { get; set; }
}

public class CCProviderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
