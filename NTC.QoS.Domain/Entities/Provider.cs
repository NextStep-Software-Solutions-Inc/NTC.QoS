using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Provider : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LicenseNumber { get; set; }
    public string Color { get; set; } = string.Empty;
    public byte[]? LogoData { get; set; }
    public string? LogoContentType { get; set; }
    public string? LogoFileName { get; set; }
    public virtual List<ServiceProvider> ServiceProviders { get; set; } = new();
}
