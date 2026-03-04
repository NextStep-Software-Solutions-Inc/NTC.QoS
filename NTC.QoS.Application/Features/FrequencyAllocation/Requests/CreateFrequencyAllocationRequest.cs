using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

namespace NTC.QoS.Application.Features.FrequencyAllocation.Requests;

public class CreateFrequencyAllocationRequest : IRequest<ApiResponse<FrequencyAllocationDto>>
{
    public required CreateFrequencyAllocationDto FrequencyAllocation { get; set; }
}
