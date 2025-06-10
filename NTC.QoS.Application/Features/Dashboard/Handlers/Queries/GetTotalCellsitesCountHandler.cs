using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTotalCellsitesCountHandler : IRequestHandler<GetTotalCellsitesCountRequest, ApiResponse<CellsiteCountsDto>>
{
    private readonly ICellRepository _cellRepository;

    public GetTotalCellsitesCountHandler(ICellRepository cellRepository)
    {
        _cellRepository = cellRepository;
    }

    public async Task<ApiResponse<CellsiteCountsDto>> Handle(GetTotalCellsitesCountRequest request, CancellationToken cancellationToken)
    {
        var cells = _cellRepository.Query(c => !c.IsDeleted).ToList();

        var verifiedCells = cells.Count(c => c.IsVerified && c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate);
        var unverifiedCells = cells.Count(c => !c.IsVerified && c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate);
        var totalCells = cells.Count;

        var responseData = new CellsiteCountsDto
        {
            VerifiedCells = verifiedCells,
            UnverifiedCells = unverifiedCells,
            TotalCells = totalCells
        };

        return await Task.FromResult(new ApiResponse<CellsiteCountsDto>
        {
            Data = responseData,
            Succeeded = true,
            Message = $"Cellsite counts between ({request.StartDate}) to ({request.EndDate}) retrieved successfully."
        });
    }
}
