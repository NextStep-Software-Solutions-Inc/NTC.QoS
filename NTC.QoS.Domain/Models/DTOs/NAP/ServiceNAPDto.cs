using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.NAP;
public class ServiceNAPDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
}
