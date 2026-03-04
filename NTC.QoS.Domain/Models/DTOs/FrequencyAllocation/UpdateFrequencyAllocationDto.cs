namespace NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

public class UpdateFrequencyAllocationDto
{
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
    public Guid? UpLinkId { get; set; }
    public Guid? DownLinkId { get; set; }
}
