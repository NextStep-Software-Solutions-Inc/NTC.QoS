using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

namespace NTC.QoS.Application.Features.FrequencyAllocation.Requests;

public class UpdateFrequencyAllocationByIdRequest : IRequest<ApiResponse<FrequencyAllocationDto>>
{
    public Guid Id { get; set; }
    public required UpdateFrequencyAllocationDto FrequencyAllocation { get; set; }
}
