using MediatR;
using NTC.QoS.Application.Features.CellSector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Handlers.Queries;

public class GetCellSectorByIdHandler(IRepository<Domain.Entities.CellSector> cellSectorRepository) : IRequestHandler<GetCellSectorByIdRequest, ApiResponse<CellSectorDto>>
{
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository = cellSectorRepository;

    public async Task<ApiResponse<CellSectorDto>> Handle(GetCellSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var cellSectorQuery = _cellSectorRepository.Query(cs => cs.Id.Equals(request.Id));

        var cellSectorDto = cellSectorQuery.Select(cellSector => new CellSectorDto
        {
            Id = cellSector.Id,
            Cell = new Domain.Models.DTOs.Cell.CellDto
            {
                Id = cellSector.Cell.Id,
                Name = cellSector.Cell.Name,
                Group = cellSector.Cell.Group,
                Description = cellSector.Cell.Description ?? string.Empty,
                Address = cellSector.Cell.Address ?? string.Empty,
                LAC = cellSector.Cell.LAC,
                TAC = cellSector.Cell.TAC,
                CID = cellSector.Cell.CID,
            },
            AntennaType = EnumHelper<AntennaTypeEnum>.GetName(cellSector.AntennaType),
            AntennaHeight = cellSector.AntennaHeight,
            Azimuth = cellSector.Azimuth,
            BeamTilt = cellSector.BeamTilt
        });

        var cellSector = await _cellSectorRepository.FirstOrDefaultAsync(cellSectorDto, cancellationToken);

        return new ApiResponse<CellSectorDto>
        {
            Data = cellSector,
            Succeeded = true,
            Message = "Cell sector retrieved successfully."
        };
    }
}
