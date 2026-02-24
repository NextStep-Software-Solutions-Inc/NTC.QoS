using MediatR;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Queries;

public class GetAllCellularHandler(ICellularRepository cellularRepository) : IRequestHandler<GetAllCellularsRequest, PaginatedResponse<CellularDto>>
{
    private readonly ICellularRepository _cellularRepository = cellularRepository;
    public async Task<PaginatedResponse<CellularDto>> Handle(GetAllCellularsRequest request, CancellationToken cancellationToken)
    {
        var allCellularsQuery = _cellularRepository.Query();

        allCellularsQuery = allCellularsQuery
            .Where(c => !request.CellSectorId.HasValue || c.CellSectorId.Equals(request.CellSectorId.Value))
            .Where(c => !request.CellId.HasValue || c.CellSector != null && c.CellSector.CellId.Equals(request.CellId.Value))
            .Where(c => !request.ServiceProviderId.HasValue || c.ServiceProviderId.Equals(request.ServiceProviderId.Value))
            .Where(c => !request.TechnologyId.HasValue || c.TechnologyId.Equals(request.TechnologyId.Value))
            .Where(c => !request.BandId.HasValue || c.BandId.Equals(request.BandId.Value));

        if (!string.IsNullOrEmpty(request.Search))
        {
            allCellularsQuery = _cellularRepository.ApplySearch(allCellularsQuery, request.Search, c => c.Address);
        }

        var cellularDtosQuery = allCellularsQuery
            .Select(c => new CellularDto
            {
                Id = c.Id,
                CellSector = c.CellSector != null ? new()
                {
                    Id = c.CellSectorId,
                    Cell = new()
                    {
                        Id = c.CellSector.Cell.Id,
                        Name = c.CellSector.Cell.Name,
                        Longitude = c.CellSector.Cell.Location.Coordinate.X,
                        Latitude = c.CellSector.Cell.Location.Coordinate.Y,
                        LAC = c.CellSector.Cell.LAC,
                        CID = c.CellSector.Cell.CID,
                    },
                    AntennaType = c.CellSector.AntennaType.ToString(),
                    AntennaHeight = c.CellSector.AntennaHeight,
                    Azimuth = c.CellSector.Azimuth,
                    BeamTilt = c.CellSector.BeamTilt
                } : null,
                ServiceProvider = c.ServiceProvider != null && c.ServiceProvider.Provider != null && c.ServiceProvider.Service != null ? new()
                {
                    Id = c.ServiceProviderId,
                    Provider = new()
                    {
                        Id = c.ServiceProvider.ProviderId,
                        Name = c.ServiceProvider.Provider.Name,
                        Color = c.ServiceProvider.Provider.Color,
                    },
                    Service = new()
                    {
                        Id = c.ServiceProvider.ServiceId,
                        Name = c.ServiceProvider.Service.Name,
                        Type = c.ServiceProvider.Service.Type.ToString(),
                    },
                } : null,
                Technology = c.Technology != null ? new()
                {
                    Id = c.TechnologyId,
                    Standard = c.Technology.Standard,
                    Description = c.Technology.Description
                } : null,
                Band = c.Band != null ? new()
                {
                    Id = c.BandId,
                    Name = c.Band.Name,
                    Description = c.Band.Description
                } : null,
                Address = c.Address
            });

        return await _cellularRepository.PaginateAsync(cellularDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
