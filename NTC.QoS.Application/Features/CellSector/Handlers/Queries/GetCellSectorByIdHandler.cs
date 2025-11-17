using MediatR;
using NTC.QoS.Application.Features.CellSector.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Handlers.Queries;

public class GetCellSectorByIdHandler(ICellSectorRepository cellSectorRepository) : IRequestHandler<GetCellSectorByIdRequest, ApiResponse<CellSectorDto>>
{
    private readonly ICellSectorRepository _cellSectorRepository = cellSectorRepository;

    public async Task<ApiResponse<CellSectorDto>> Handle(GetCellSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var cellSectorQuery = _cellSectorRepository.Query(cs => cs.Id.Equals(request.Id));
        var cellSector = await _cellSectorRepository.FirstOrDefaultAsync(cellSectorQuery, cancellationToken);

        if (cellSector is null)
        {
            return new ApiResponse<CellSectorDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullCellSector", ["Cell Sector not found."]
                    },
                },
                Message = "Cell Sector not found."
            };
        }

        var cellSectorDto = new CellSectorDto
        {
            Id = cellSector.Id,
            Cell = new()
            {
                Id = cellSector.Cell.Id,
                Name = cellSector.Cell.Name,
                Group = cellSector.Cell.Group,
                Description = cellSector.Cell.Description,
                Address = cellSector.Cell.Address,
                LAC = cellSector.Cell.LAC,
                TAC = cellSector.Cell.TAC,
                CID = cellSector.Cell.CID,
            },
            AntennaType = cellSector.AntennaType.ToString(),
            AntennaHeight = cellSector.AntennaHeight,
            Azimuth = cellSector.Azimuth,
            BeamTilt = cellSector.BeamTilt
        };

        return new ApiResponse<CellSectorDto>
        {
            Data = cellSectorDto,
            Succeeded = true,
            Message = "Cell sector retrieved successfully."
        };
    }
}
