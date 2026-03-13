using MediatR;
using NTC.QoS.Application.Features.FrequencyAllocation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.FrequencyAllocation.Handlers.Commands;

public class DeleteFrequencyAllocationByIdHandler(IFrequencyAllocationRepository repository)
    : IRequestHandler<DeleteFrequencyAllocationByIdRequest, ApiResponse<bool>>
{
    private readonly IFrequencyAllocationRepository _repository = repository;

    public async Task<ApiResponse<bool>> Handle(
        DeleteFrequencyAllocationByIdRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.FirstOrDefaultAsync(
            _repository.Query(fa => fa.Id == request.Id), cancellationToken);

        if (entity is null)
            return new ApiResponse<bool> { Succeeded = false, Message = "Frequency allocation not found.", Data = false };

        _repository.Delete(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool> { Succeeded = true, Message = "Frequency allocation deleted successfully.", Data = true };
    }
}
