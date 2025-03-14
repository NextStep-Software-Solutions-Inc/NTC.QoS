using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class NetworkSectorDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required NetworkSystemTypeEnum SystemType { get; set; }
}
