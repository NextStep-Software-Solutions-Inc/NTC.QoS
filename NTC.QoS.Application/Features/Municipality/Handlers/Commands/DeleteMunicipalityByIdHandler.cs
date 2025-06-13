using MediatR;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Commands;

public class DeleteMunicipalitiesByIdsHandler(IMunicipalityRepository repository) : IRequestHandler<DeleteMunicipalitiesByIdsRequest, ApiResponse<bool>>
{
    private readonly IMunicipalityRepository _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteMunicipalitiesByIdsRequest request, CancellationToken cancellationToken)
    {
        var municipalitiesToDeleteQuery = _repository.Query(m => request.Ids.Contains(m.Id) && !m.IsDeleted);
        var municipalitiesToDeleteCount = await _repository.ExecuteDeleteAsync(municipalitiesToDeleteQuery, cancellationToken);

        if (municipalitiesToDeleteCount is 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified municipalities exist or are already deleted."
            };
        }

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = $"{municipalitiesToDeleteCount} municipalities deleted successfully."
        };
    }
}
