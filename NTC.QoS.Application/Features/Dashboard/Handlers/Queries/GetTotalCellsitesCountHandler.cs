using MediatR;
using Microsoft.EntityFrameworkCore;
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
        var cellsQuery = _cellRepository.Query();

        if (request.StartDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt <= request.EndDate.Value);

        var verifiedCells = await cellsQuery.CountAsync(c => c.IsVerified, cancellationToken);
        var unverifiedCells = await cellsQuery.CountAsync(c => !c.IsVerified, cancellationToken);
        var totalCells = await cellsQuery.CountAsync(cancellationToken);

        var responseData = new CellsiteCountsDto
        {
            VerifiedCells = verifiedCells,
            UnverifiedCells = unverifiedCells,
            TotalCells = totalCells
        };

        string message;
        if (request.StartDate.HasValue && request.EndDate.HasValue)
            message = $"Cellsite counts between ({request.StartDate.Value}) to ({request.EndDate.Value}) retrieved successfully.";
        else if (request.StartDate.HasValue)
            message = $"Cellsite counts from ({request.StartDate.Value}) onwards retrieved successfully.";
        else if (request.EndDate.HasValue)
            message = $"Cellsite counts up to ({request.EndDate.Value}) retrieved successfully.";
        else
            message = "Total cellsite counts retrieved successfully (no date filter applied).";

        return new ApiResponse<CellsiteCountsDto>
        {
            Data = responseData,
            Succeeded = true,
            Message = message
        };
    }
}
