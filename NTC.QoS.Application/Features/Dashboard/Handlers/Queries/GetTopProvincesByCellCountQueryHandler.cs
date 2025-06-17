using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Dashboard_1;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopProvincesByCellCountQueryHandler(
  IProvinceRepository provinceRepository,
  ICellRepository cellRepository,
  IGeoSpatialService geoSpatialService)
  : IRequestHandler<GetTopProvincesByCellCountQueryRequest, List<TopProvinceCellCountDto>>
{
    private readonly IProvinceRepository _provinceRepository = provinceRepository;
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IGeoSpatialService _geoSpatialService = geoSpatialService;

    public async Task<List<TopProvinceCellCountDto>> Handle(GetTopProvincesByCellCountQueryRequest request, CancellationToken cancellationToken)
    {
        // Get all provinces for the selected region (with boundaries)
        var provinces = await _provinceRepository.Query(p => p.RegionId == request.RegionId && p.Boundaries != null).ToListAsync(cancellationToken);

        // Get all cells (with location only)
        var cells = await _cellRepository.Query(c => c.Location != null).Select(c => new { c.Id, c.Location }).ToListAsync(cancellationToken);

        var results = provinces
          .Select(p => new TopProvinceCellCountDto
          {
              ProvinceId = p.Id,
              ProvinceName = p.Name,
              CellCount = cells.Count(c => _geoSpatialService.IsPointWithinProvince(c.Location, p))
          })
          .OrderByDescending(r => r.CellCount)
          .Take(request.Top)
          .ToList();

        return results ?? new List<TopProvinceCellCountDto>();
    }
}