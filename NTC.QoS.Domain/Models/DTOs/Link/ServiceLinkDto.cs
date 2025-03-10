namespace NTC.QoS.Domain.Models.DTOs.Link;

public class ServiceLinkDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
}
