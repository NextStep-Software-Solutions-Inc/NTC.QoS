namespace NTC.QoS.Domain.Models.DTOs.Link;

public class ServiceLinkDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
