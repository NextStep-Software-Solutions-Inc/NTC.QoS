using NTC.QoS.Domain.Models.DTOs.Link;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

public class FrequencyAllocationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public ProviderDto? Provider { get; set; }
    public LinkDto? UpLink { get; set; }
    public LinkDto? DownLink { get; set; }
}
