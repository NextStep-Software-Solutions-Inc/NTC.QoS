using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopProvincesInRegionByCellCountHandler2(ICellRepository cellRepository, IProvinceRepository provinceRepository) : IRequestHandler<GetTopProvincesInRegionByCellCountRequest2, ApiResponse<List<TopProvincesInRegionByCellCountDto>>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IProvinceRepository _provinceRepository = provinceRepository;

    public async Task<ApiResponse<List<TopProvincesInRegionByCellCountDto>>> Handle(GetTopProvincesInRegionByCellCountRequest2 request, CancellationToken cancellationToken)
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

        var provinceQuery = _provinceRepository.Query().Where(p => p.Boundaries != null);

        if (request.RegionId.HasValue)
        {
            provinceQuery = provinceQuery.Where(p => p.RegionId == request.RegionId.Value);
        }

        var result = await provinceQuery
            .Select(province => new TopProvincesInRegionByCellCountDto
            {
                RegionId = province.RegionId,
                Id = province.Id,
                Name = province.Name,
                Code = province.Code,
                CellCount = cellsQuery.Count(cell =>
                    cell.Location.IsWithinDistance(province.Boundaries, 0)
                )
            })
            .OrderByDescending(dto => dto.CellCount)
            .Take(request.Limit > 0 ? request.Limit : int.MaxValue)
            .ToListAsync(cancellationToken);

        string message = request.RegionId.HasValue
            ? $"Top provinces in RegionId [{request.RegionId}] retrieved successfully."
            : "Top provinces across all regions retrieved successfully.";

        return new ApiResponse<List<TopProvincesInRegionByCellCountDto>>
        {
            Succeeded = true,
            Message = message,
            Data = result
        };
    }
}
