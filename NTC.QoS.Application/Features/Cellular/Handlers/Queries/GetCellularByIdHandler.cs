using MediatR;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Queries;

public class GetCellularByIdHandler(ICellularRepository cellularRepository) : IRequestHandler<GetCellularByIdRequest, ApiResponse<CellularDto>>
{
    private readonly ICellularRepository _cellularRepository = cellularRepository;

    public async Task<ApiResponse<CellularDto>> Handle(GetCellularByIdRequest request, CancellationToken cancellationToken)
    {
        var cellularDtoQuery = _cellularRepository.Query(c => c.Id.Equals(request.Id))
            .Select(cellular => new CellularDto
            {
                Id = cellular.Id,
                CellSector = cellular.CellSector != null ? new()
                {
                    Id = cellular.CellSectorId,
                    Cell = new()
                    {
                        Id = cellular.CellSector.Cell.Id,
                        Name = cellular.CellSector.Cell.Name,
                        Description = cellular.CellSector.Cell.Description,
                        Longitude = cellular.CellSector.Cell.Location.Coordinate.X,
                        Latitude = cellular.CellSector.Cell.Location.Coordinate.Y,
                        LAC = cellular.CellSector.Cell.LAC,
                        CID = cellular.CellSector.Cell.CID
                    },
                    AntennaType = cellular.CellSector.AntennaType.ToString(),
                    AntennaHeight = cellular.CellSector.AntennaHeight,
                    Azimuth = cellular.CellSector.Azimuth,
                    BeamTilt = cellular.CellSector.BeamTilt
                } : null,
                ServiceProvider = cellular.ServiceProvider != null ? new()
                {
                    Id = cellular.ServiceProviderId,
                    Provider = new()
                    {
                        Id = cellular.ServiceProvider.ProviderId,
                        Name = cellular.ServiceProvider.Provider.Name,
                        Color = cellular.ServiceProvider.Provider.Color
                    },
                    Service = new()
                    {
                        Id = cellular.ServiceProvider.ServiceId,
                        Name = cellular.ServiceProvider.Service.Name,
                        Type = cellular.ServiceProvider.Service.Type.ToString()
                    }
                } : null,
                Technology = cellular.Technology != null ? new()
                {
                    Id = cellular.TechnologyId,
                    Standard = cellular.Technology.Standard,
                    Description = cellular.Technology.Description
                } : null,
                Band = cellular.Band != null ? new()
                {
                    Id = cellular.BandId,
                    Name = cellular.Band.Name,
                    Description = cellular.Band.Description
                } : null,
                Address = cellular.Address
            });

        var cellularDto = await _cellularRepository.FirstOrDefaultAsync(cellularDtoQuery, cancellationToken);

        if (cellularDto is null)
        {
            return new ApiResponse<CellularDto>
            {
                Succeeded = false,
                Message = "Cellular record not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Cellular record with the specified ID does not exist."] }
                }
            };
        }

        return new ApiResponse<CellularDto>
        {
            Succeeded = true,
            Message = "Cellular record retrieved successfully.",
            Data = cellularDto
        };
    }
}