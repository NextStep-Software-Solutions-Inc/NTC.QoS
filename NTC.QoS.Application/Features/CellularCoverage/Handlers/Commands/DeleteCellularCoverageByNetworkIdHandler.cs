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
        var cellularCoveragesToDeleteQuery = _cellularCoverageRepository.Query(cs => !cs.IsDeleted && cs.NetworkId == request.NetworkId);
        var cellularCoveragesToDelete = await _cellularCoverageRepository.ToListAsync(cellularCoveragesToDeleteQuery, cancellationToken);
        
        if (cellularCoveragesToDelete.Count == 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "No cellular coverage found for the given Network ID."
            };
        }

        _cellularCoverageRepository.RemoveRange(cellularCoveragesToDelete);
        await _cellularCoverageRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = $"{cellularCoveragesToDelete.Count()} cellular coverage entries deleted successfully for Network ID: {request.NetworkId}."
        };
    }
}
