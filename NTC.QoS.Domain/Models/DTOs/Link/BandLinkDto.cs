namespace NTC.QoS.Domain.Models.DTOs.Link;

public class BandLinkDto
{
    //Band details
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}
