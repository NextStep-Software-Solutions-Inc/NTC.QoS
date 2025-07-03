using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopBarangaysInMunicipalityByCellCountHandler2(ICellRepository cellRepository, IBarangayRepository barangayRepository) : IRequestHandler<GetTopBarangaysInMunicipalityByCellCountRequest2, ApiResponse<List<TopBarangaysInMunicipalitiesByCellCountDto>>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;

    public async Task<ApiResponse<List<TopBarangaysInMunicipalitiesByCellCountDto>>> Handle(GetTopBarangaysInMunicipalityByCellCountRequest2 request, CancellationToken cancellationToken)
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

        var barangayQuery = _barangayRepository.Query().Where(b => b.Boundaries != null);

        if (request.MunicipalityId.HasValue)
        {
            barangayQuery = barangayQuery.Where(b => b.MunicipalityId == request.MunicipalityId.Value);
        }

        var result = await barangayQuery
            .Select(barangay => new TopBarangaysInMunicipalitiesByCellCountDto
            {
                MunicipalityId = barangay.MunicipalityId,
                Id = barangay.Id,
                Name = barangay.Name,
                Code = barangay.Code,
                CellCount = cellsQuery.Count(cell =>
                    cell.Location.IsWithinDistance(barangay.Boundaries, 0)
                )
            })
            .OrderByDescending(dto => dto.CellCount)
            .Take(request.Limit > 0 ? request.Limit : int.MaxValue)
            .ToListAsync(cancellationToken);

        string message = request.MunicipalityId.HasValue
            ? $"Top barangays in MunicipalityId [{request.MunicipalityId}] retrieved successfully."
            : "Top provinces across all regions retrieved successfully.";

        return new ApiResponse<List<TopBarangaysInMunicipalitiesByCellCountDto>>
        {
            Succeeded = true,
            Message = message,
            Data = result
        };
    }
}
