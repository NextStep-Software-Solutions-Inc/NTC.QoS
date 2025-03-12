namespace NTC.QoS.Domain.Models.DTOs.Network;

public class LinkNetworkDto
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public decimal CenterFrequency { get; set; }
}
