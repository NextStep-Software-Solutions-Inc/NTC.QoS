using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

public class GetTopRegionsByCellCountHandler2 : IRequestHandler<GetTopRegionsByCellCountRequest2, ApiResponse<List<TopRegionsByCellCountDto>>>
{
    private readonly ICellRepository _cellRepository;
    private readonly IRegionRepository _regionRepository;
    public GetTopRegionsByCellCountHandler2(ICellRepository cellRepository, IRegionRepository regionRepository)
    {
        _cellRepository = cellRepository;
        _regionRepository = regionRepository;
    }

    public async Task<ApiResponse<List<TopRegionsByCellCountDto>>> Handle(GetTopRegionsByCellCountRequest2 request, CancellationToken cancellationToken)
    {
        var cellsQuery = _cellRepository.Query().Where(c => c.Location != null);

        if (request.StartDate.HasValue)
        {
            cellsQuery = cellsQuery.Where(c => c.CreatedAt >= request.StartDate.Value);
        }
        if (request.EndDate.HasValue)
        {
            cellsQuery = cellsQuery.Where(c => c.CreatedAt <= request.EndDate.Value);
        }

        var result = await _regionRepository.Query()
            .Where(r => r.Boundaries != null)
            .Select(region => new TopRegionsByCellCountDto
            {
                Id = region.Id,
                Name = region.Name,
                Code = region.Code,
                CellCount = cellsQuery.Count(cell =>
                    cell.Location.IsWithinDistance(region.Boundaries, 0)
                )
            })
            .OrderByDescending(dto => dto.CellCount)
            .Take(request.Limit > 0 ? request.Limit : int.MaxValue)
            .ToListAsync(cancellationToken);

        return new ApiResponse<List<TopRegionsByCellCountDto>>
        {
            Succeeded = true,
            Message = "Top regions retrieved successfully.",
            Data = result
        };
    }
}
