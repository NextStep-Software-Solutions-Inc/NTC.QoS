using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class ServiceCellularDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
