using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

namespace NTC.QoS.Application.Features.FrequencyAllocation.Requests;

public class GetAllFrequencyAllocationsRequest : IRequest<PaginatedResponse<FrequencyAllocationDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public List<Guid>? ProviderIds { get; set; }
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
}
