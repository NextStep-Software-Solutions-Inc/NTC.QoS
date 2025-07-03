using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopMunicipalitiesInProvinceByCellCountHandler2(ICellRepository cellRepository, IMunicipalityRepository municipalityRepository) : IRequestHandler<GetTopMunicipalitiesInProvinceByCellCountRequest2, ApiResponse<List<TopMunicipalitiesInProvinceByCellCountDto>>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;

    public async Task<ApiResponse<List<TopMunicipalitiesInProvinceByCellCountDto>>> Handle(GetTopMunicipalitiesInProvinceByCellCountRequest2 request, CancellationToken cancellationToken)
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

        var municipalityQuery = _municipalityRepository.Query().Where(m => m.Boundaries != null);

        if (request.ProvinceId.HasValue)
        {
            municipalityQuery = municipalityQuery.Where(m => m.ProvinceId == request.ProvinceId.Value);
        }

        var result = await municipalityQuery
            .Select(province => new TopMunicipalitiesInProvinceByCellCountDto
            {
                ProvinceId = province.ProvinceId,
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

        string message = request.ProvinceId.HasValue
            ? $"Top municipalities in ProvinceId [{request.ProvinceId}] retrieved successfully."
            : "Top provinces across all regions retrieved successfully.";

        return new ApiResponse<List<TopMunicipalitiesInProvinceByCellCountDto>>
        {
            Succeeded = true,
            Message = message,
            Data = result
        };
    }
}
