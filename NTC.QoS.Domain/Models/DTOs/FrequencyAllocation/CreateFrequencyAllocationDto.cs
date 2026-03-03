namespace NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

public class CreateFrequencyAllocationDto
{
    public required string Name { get; set; }
    public string? Notes { get; set; }
    public Guid ProviderId { get; set; }
    public Guid UpLinkId { get; set; }
    public Guid DownLinkId { get; set; }
}
