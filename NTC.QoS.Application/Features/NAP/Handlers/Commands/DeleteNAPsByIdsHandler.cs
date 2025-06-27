using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.NAP.Handlers.Commands;

public class DeleteNAPsByIdsHandler(IRepository<Domain.Aggregates.NAP> repository) : IRequestHandler<DeleteNAPsByIdsRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Aggregates.NAP> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteNAPsByIdsRequest request, CancellationToken cancellationToken)
    {
        var NAPsToDeleteQuery = _repository.Query(n => !n.IsDeleted && request.Ids.Contains(n.Id));
        var NAPsDeletedCount = await _repository.ExecuteDeleteAsync(NAPsToDeleteQuery, cancellationToken);

        if (NAPsDeletedCount == 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified NAPs exist or are already deleted."
            };
        }

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = $"{NAPsDeletedCount} NAPs deleted successfully."
        };
    }
}
