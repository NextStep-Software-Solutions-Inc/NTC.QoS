using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Dashboard_1;
using System.Linq;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopProvincesByCellCountQueryHandler(
    IProvinceRepository provinceRepository,
    ICellRepository cellRepository)
    : IRequestHandler<GetTopProvincesByCellCountQueryRequest, List<TopProvinceCellCountDto>>
{
    private readonly IProvinceRepository _provinceRepository = provinceRepository;
    private readonly ICellRepository _cellRepository = cellRepository;

    public async Task<List<TopProvinceCellCountDto>> Handle(GetTopProvincesByCellCountQueryRequest request, CancellationToken cancellationToken)
    {
        var provinces = await _provinceRepository.Query(p => p.RegionId == request.RegionId && p.Boundaries != null)
            .ToListAsync(cancellationToken);

        if (!provinces.Any())
        {
            return new List<TopProvinceCellCountDto>();
        }

        var results = new List<TopProvinceCellCountDto>();

        foreach (var province in provinces)
        {
            // Use IsWithinDistance with a tolerance of 0 for containment checks with geography types
            // This translates to ST_DWithin(c.Location, province.Boundaries, 0) in PostGIS.
            // A tolerance of 0 means the point must be exactly within or on the boundary.
            var cellCount = await _cellRepository.Query()
                                                .CountAsync(c => c.Location != null && c.Location.IsWithinDistance(province.Boundaries, 0), cancellationToken);

            results.Add(new TopProvinceCellCountDto
            {
                ProvinceId = province.Id,
                ProvinceName = province.Name,
                CellCount = cellCount
            });
        }

        var topProvinces = results
            .OrderByDescending(r => r.CellCount)
            .Take(request.Top)
            .ToList();

        return topProvinces;
    }
}