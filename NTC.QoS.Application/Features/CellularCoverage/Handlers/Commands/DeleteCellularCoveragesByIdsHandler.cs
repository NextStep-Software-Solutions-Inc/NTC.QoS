using MediatR;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Commands;

public class DeleteCellularCoveragesByIdsHandler(IRepository<Domain.Entities.CellularCoverage> repository) : IRequestHandler<DeleteCellularCoveragesByIdsRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.CellularCoverage> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteCellularCoveragesByIdsRequest request, CancellationToken cancellationToken)
    {
        var cellularCoveragesToDeleteQuery = _repository.Query(cc => !cc.IsDeleted && request.Ids.Contains(cc.Id));
        var cellularCoveragesDeletedCount = await _repository.ExecuteDeleteAsync(cellularCoveragesToDeleteQuery, cancellationToken);

        if (cellularCoveragesDeletedCount == 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified cellular coverage/s exist or are already deleted."
            };
        }

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = $"{cellularCoveragesDeletedCount} cellular coverages deleted successfully."
        };
    }
}
