namespace NTC.QoS.Domain.Models.DTOs.Link;

public class ProviderLinkDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}
