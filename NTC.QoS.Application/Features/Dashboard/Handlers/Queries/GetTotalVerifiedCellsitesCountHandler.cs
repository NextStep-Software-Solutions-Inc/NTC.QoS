using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTotalVerifiedCellsitesCountHandler(ICellRepository cellRepository) : IRequestHandler<GetTotalVerifiedCellsitesCountRequest, ApiResponse<int>>
{
    private readonly ICellRepository _cellRepository = cellRepository;

    public async Task<ApiResponse<int>> Handle(GetTotalVerifiedCellsitesCountRequest request, CancellationToken cancellationToken)
    {
        var verifiedCells = _cellRepository.Query(c => !c.IsDeleted && c.IsVerified.Equals(true) && 
            c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate).Count();

        return await Task.FromResult(new ApiResponse<int>
        {
            Data = verifiedCells,
            Succeeded = true,
            Message = "Total verified cellsites count retrieved successfully."
        });
    }
}
