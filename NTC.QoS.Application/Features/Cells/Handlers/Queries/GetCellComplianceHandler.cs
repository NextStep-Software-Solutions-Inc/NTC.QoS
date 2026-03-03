using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetCellComplianceHandler(
    ICellularRepository cellularRepository,
    IFrequencyAllocationRepository allocationRepository)
    : IRequestHandler<GetCellComplianceRequest, ApiResponse<CellComplianceDto>>
{
    private readonly ICellularRepository _cellularRepository = cellularRepository;
    private readonly IFrequencyAllocationRepository _allocationRepository = allocationRepository;

    public async Task<ApiResponse<CellComplianceDto>> Handle(
        GetCellComplianceRequest request,
        CancellationToken cancellationToken)
    {
        var cellularEntries = await _cellularRepository.ToListAsync(
            _cellularRepository.Query(c => c.CellSector != null && c.CellSector.CellId == request.CellId)
                .Select(c => new
                {
                    c.ServiceProviderId,
                    ProviderId   = c.ServiceProvider != null ? c.ServiceProvider.ProviderId : Guid.Empty,
                    ProviderName = c.ServiceProvider != null && c.ServiceProvider.Provider != null
                        ? c.ServiceProvider.Provider.Name
                        : string.Empty,
                    c.TechnologyId,
                    TechnologyStandard = c.Technology != null ? c.Technology.Standard : string.Empty,
                    c.BandId,
                    BandName = c.Band != null ? c.Band.Name : string.Empty,
                }),
            cancellationToken);

        if (cellularEntries.Count == 0)
            return new ApiResponse<CellComplianceDto>
            {
                Succeeded = false,
                Message   = "No cellular records found for the given cell."
            };

        var distinctEntries = cellularEntries
            .GroupBy(c => (c.ServiceProviderId, c.TechnologyId, c.BandId))
            .Select(g => g.First())
            .ToList();

        var providerIds = distinctEntries.Select(e => e.ProviderId).Distinct().ToList();

        var allocations = await _allocationRepository.ToListAsync(
            _allocationRepository.Query(fa =>
                fa.IsActive && providerIds.Contains(fa.ProviderId))
                .Select(fa => new
                {
                    fa.ProviderId,
                    fa.Name,
                    fa.Notes,
                    UpLinkBandId   = fa.UpLink   != null ? fa.UpLink.BandId   : Guid.Empty,
                    DownLinkBandId = fa.DownLink != null ? fa.DownLink.BandId : Guid.Empty,
                }),
            cancellationToken);

        var items = new List<CellBandComplianceItemDto>();

        foreach (var entry in distinctEntries)
        {
            var matched = allocations.FirstOrDefault(fa =>
                fa.ProviderId == entry.ProviderId &&
                (fa.UpLinkBandId == entry.BandId || fa.DownLinkBandId == entry.BandId));

            items.Add(new CellBandComplianceItemDto
            {
                ServiceProviderId      = entry.ServiceProviderId,
                ProviderName           = entry.ProviderName,
                TechnologyId           = entry.TechnologyId,
                TechnologyStandard     = entry.TechnologyStandard,
                BandId                 = entry.BandId,
                BandName               = entry.BandName,
                IsAuthorized           = matched is not null,
                MatchedAllocationName  = matched?.Name,
                MatchedAllocationNotes = matched?.Notes,
            });
        }

        var overallStatus = allocations.Count == 0
            ? ComplianceStatus.Unknown
            : items.Any(i => !i.IsAuthorized)
                ? ComplianceStatus.Warning
                : ComplianceStatus.Compliant;

        return new ApiResponse<CellComplianceDto>
        {
            Succeeded = true,
            Message   = "Cell compliance check completed.",
            Data      = new CellComplianceDto
            {
                CellId        = request.CellId,
                OverallStatus = overallStatus,
                Items         = items,
            }
        };
    }
}
