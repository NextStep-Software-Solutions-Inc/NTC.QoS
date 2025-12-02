using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;
using NTC.QoS.Application.Features.CellularCoverage.Requests;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Queries;

/// <summary>
/// Handler for querying CellularCoverage V2 records
/// </summary>
public class GetCellularCoverageV2Handler : IRequestHandler<GetCellularCoverageV2Query, PaginatedResponse<CellularCoverageV2Dto>>
{
    private readonly ICellularCoverageV2Repository _repository;

    public GetCellularCoverageV2Handler(ICellularCoverageV2Repository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedResponse<CellularCoverageV2Dto>> Handle(
        GetCellularCoverageV2Query request,
        CancellationToken cancellationToken)
    {
        var query = _repository.Query(cc => !cc.IsDeleted);

        // Apply filters
        if (request.MunicipalityId.HasValue)
        {
            query = query.Where(cc => cc.MunicipalityId == request.MunicipalityId.Value);
        }

        if (request.NetworkId.HasValue)
        {
            query = query.Where(cc => cc.NetworkId == request.NetworkId.Value);
        }

        if (request.BandId.HasValue)
        {
            query = query.Where(cc => cc.BandId == request.BandId.Value);
        }

        if (request.ServiceProviderId.HasValue)
        {
            query = query.Where(cc => cc.CellSector.Cell.ServiceProviderId == request.ServiceProviderId.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .OrderByDescending(cc => cc.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(cc => new CellularCoverageV2Dto
            {
                Id = cc.Id,
                NetworkId = cc.NetworkId,
                NetworkName = cc.Network.Name,
                BandId = cc.BandId,
                BandName = cc.Band.Name,
                TechnologyId = cc.TechnologyId,
                TechnologyName = cc.Technology.Standard,
                CellSectorId = cc.CellSectorId,
                CellSectorName = cc.CellSector.Cell.Name,
                MunicipalityId = cc.MunicipalityId,
                MunicipalityName = cc.Municipality.Name,
                Lat = cc.Location.Y,
                Lng = cc.Location.X,
                SignalStrength = cc.SignalStrength,
                Address = cc.Address,
                IsFileUploaded = cc.IsFileUploaded,
                CreatedAt = cc.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<CellularCoverageV2Dto>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }
}
