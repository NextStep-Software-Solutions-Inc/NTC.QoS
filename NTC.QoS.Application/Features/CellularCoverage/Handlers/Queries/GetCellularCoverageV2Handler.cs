using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;
using NTC.QoS.Application.Features.CellularCoverage.Requests;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Queries;

/// <summary>
/// Handler for querying CellularCoverage V2 records
/// </summary>
public class GetCellularCoverageV2Handler : IRequestHandler<GetCellularCoverageV2Query, PaginatedResponse<CellularCoverageV2Dto>>
{
    private readonly ICellularCoverageRepository _repository;

    public GetCellularCoverageV2Handler(ICellularCoverageRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedResponse<CellularCoverageV2Dto>> Handle(
        GetCellularCoverageV2Query request,
        CancellationToken cancellationToken)
    {
        var query = _repository.QueryAsNoTracking(cc => !cc.IsDeleted);

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
            query = query.Where(cc => cc.CellSector != null && cc.CellSector.Cell.ServiceProviderId == request.ServiceProviderId.Value);
        }

        if (request.CellId.HasValue)
        {
            query = query.Where(cc => cc.CellSector != null && cc.CellSector.CellId == request.CellId.Value);
        }

        // Get total count
        var totalCount = await _repository.CountAsync(query, cancellationToken);

        // Apply pagination and projection
        var paginatedQuery = query
            .OrderByDescending(cc => cc.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(cc => new CellularCoverageV2Dto
            {
                Id = cc.Id,
                NetworkId = cc.NetworkId,
                NetworkName = cc.Network.Name,
                BandId = cc.BandId!.Value,
                BandName = cc.Band!.Name,
                TechnologyId = cc.TechnologyId!.Value,
                TechnologyName = cc.Technology!.Standard,
                CellSectorId = cc.CellSectorId!.Value,
                CellSectorName = cc.CellSector!.Cell.Name,
                MunicipalityId = cc.MunicipalityId!.Value,
                MunicipalityName = cc.Municipality!.Name,
                Lat = cc.Location.Y,
                Lng = cc.Location.X,
                SignalStrength = cc.SignalStrength,
                Address = cc.Address,
                IsFileUploaded = cc.IsFileUploaded,
                CreatedAt = cc.CreatedAt
            });

        var items = await _repository.ToListAsync(paginatedQuery, cancellationToken);

        return new PaginatedResponse<CellularCoverageV2Dto>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }
}
