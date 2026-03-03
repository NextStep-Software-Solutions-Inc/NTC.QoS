using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetCellTechnologiesHandler(ICellularRepository cellularRepository)
    : IRequestHandler<GetCellTechnologiesRequest, ApiResponse<List<CellTechnologyBandDto>>>
{
    private readonly ICellularRepository _cellularRepository = cellularRepository;

    public async Task<ApiResponse<List<CellTechnologyBandDto>>> Handle(
        GetCellTechnologiesRequest request,
        CancellationToken cancellationToken)
    {
        var rows = await _cellularRepository.Query()
            .Where(c => c.CellSector != null && c.CellSector.CellId == request.CellId)
            .Select(c => new
            {
                TechnologyId      = c.TechnologyId,
                TechnologyStandard = c.Technology != null ? c.Technology.Standard : string.Empty,
                TechnologyDesc    = c.Technology != null ? c.Technology.Description : null,
                BandId            = c.BandId,
                BandName          = c.Band != null ? c.Band.Name : string.Empty,
                BandDesc          = c.Band != null ? c.Band.Description : null,
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var grouped = rows
            .GroupBy(r => r.TechnologyId)
            .Select(g => new CellTechnologyBandDto
            {
                TechnologyId          = g.Key,
                TechnologyStandard    = g.First().TechnologyStandard,
                TechnologyDescription = g.First().TechnologyDesc,
                Bands = g
                    .DistinctBy(r => r.BandId)
                    .OrderBy(r => r.BandName)
                    .Select(r => new CellBandItemDto
                    {
                        BandId          = r.BandId,
                        BandName        = r.BandName,
                        BandDescription = r.BandDesc,
                    })
                    .ToList()
            })
            .OrderBy(t => t.TechnologyStandard)
            .ToList();

        return new ApiResponse<List<CellTechnologyBandDto>>
        {
            Succeeded = true,
            Message   = "Cell technologies retrieved successfully.",
            Data      = grouped
        };
    }
}
