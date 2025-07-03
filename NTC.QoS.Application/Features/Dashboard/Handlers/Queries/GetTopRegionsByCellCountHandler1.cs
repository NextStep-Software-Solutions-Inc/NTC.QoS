using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;
using NTC.QoS.Infrastructure.Persistence;

public class GetTopRegionsByCellCountHandler1 : IRequestHandler<GetTopRegionsByCellCountRequest1, ApiResponse<List<GeoEntityCellCountDto>>>
{
    private readonly ApplicationDbContext _dbContext;

    public GetTopRegionsByCellCountHandler1(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<GeoEntityCellCountDto>>> Handle(GetTopRegionsByCellCountRequest1 request, CancellationToken cancellationToken)
    {
        var cellsBaseQuery = _dbContext.Cells
            .Where(c => c.Location != null); // Only consider cells with a valid location

        if (request.StartDate.HasValue)
        {
            cellsBaseQuery = cellsBaseQuery.Where(c => c.CreatedAt >= request.StartDate.Value);
        }
        if (request.EndDate.HasValue)
        {
            cellsBaseQuery = cellsBaseQuery.Where(c => c.CreatedAt <= request.EndDate.Value);
        }

        var result = await _dbContext.Regions
            .Where(r => r.Boundaries != null) // Only consider regions that have defined boundaries
            .Select(region => new GeoEntityCellCountDto // <--- THIS IS THE CRUCIAL CHANGE
            {
                Id = region.Id,
                Name = region.Name,
                Code = region.Code,
                EntityType = "Region", // Static string for the Region entity
                CellCount = cellsBaseQuery.Count(cell =>
                    cell.Location.IsWithinDistance(region.Boundaries, 0)
                )
            })
            .OrderByDescending(dto => dto.CellCount)
            .Take(request.Limit > 0 ? request.Limit : int.MaxValue)
            .ToListAsync(cancellationToken);

        return new ApiResponse<List<GeoEntityCellCountDto>>
        {
            Succeeded = true,
            Message = "Top regions retrieved successfully.",
            Data = result
        };
    }
}
