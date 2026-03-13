using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;

namespace NTC.QoS.Application.Features.FrequencyAllocation.Requests;

public class GetFrequencyAllocationByIdRequest(Guid id) : IRequest<ApiResponse<FrequencyAllocationDto>>
{
    public Guid Id { get; set; } = id;
}
