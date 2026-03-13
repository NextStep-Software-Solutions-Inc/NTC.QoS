using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.FrequencyAllocation.Requests;

public class DeleteFrequencyAllocationByIdRequest(Guid id) : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; } = id;
}
