using MediatR;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Commands;

public class DeleteCellularCoverageByNetworkIdHandler(IRepository<Domain.Entities.CellularCoverage> cellularCoverageRepository) : IRequestHandler<DeleteCellularCoverageByNetworkIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.CellularCoverage> _cellularCoverageRepository = cellularCoverageRepository;

    public async Task<ApiResponse<bool>> Handle(DeleteCellularCoverageByNetworkIdRequest request, CancellationToken cancellationToken)
    {
        var cellularCoveragesToDeleteQuery = _cellularCoverageRepository.Query(cs => cs.NetworkId == request.NetworkId);
        var deletedCount = await _cellularCoverageRepository.ExecuteHardDeleteAsync(cellularCoveragesToDeleteQuery, cancellationToken);

        if (deletedCount is 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified cellular coverage/s exist or are already deleted for the given NetworkId."
            };
        }

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = $"{deletedCount} cellular coverage entries deleted successfully for Network ID: {request.NetworkId}."
        };
    }
}
