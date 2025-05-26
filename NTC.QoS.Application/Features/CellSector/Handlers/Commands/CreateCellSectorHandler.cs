using MediatR;
using NTC.QoS.Application.Features.CellSector.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.CellSector.Handlers.Commands;

public class CreateCellSectorHandler(IRepository<Domain.Entities.CellSector> cellSectorRepository, ICellRepository cellRepository) : IRequestHandler<CreateCellSectorRequest, ApiResponse<CellSectorDto>>
{
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository = cellSectorRepository;
    private readonly ICellRepository _cellRepository = cellRepository;

    public async Task<ApiResponse<CellSectorDto>> Handle(CreateCellSectorRequest request, CancellationToken cancellationToken)
    {
        var cellQuery = _cellRepository.Query(c => !c.IsDeleted);
        var cell = await _cellRepository.FirstOrDefaultAsync(cellQuery, cancellationToken);
        if (cell is null)
        {
            return new ApiResponse<CellSectorDto>
            {
                Message = "Cell does not exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Invalid Id", ["Cell not found."]
                    }
                },
            };
        }

        var newCellSector = new Domain.Entities.CellSector
        {
            CellId = cell.Id,
            AntennaType = EnumHelper<AntennaTypeEnum>.ParseUserInput(request.CellSector.AntennaType),
            AntennaHeight = request.CellSector.AntennaHeight,
            Azimuth = request.CellSector.Azimuth,
            BeamTilt = request.CellSector.BeamTilt,
        };

        _cellSectorRepository.Add(newCellSector);
        await _cellSectorRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CellSectorDto>
        {
            Data = new CellSectorDto
            {
                Id = newCellSector.Id,
                Cell = new Domain.Models.DTOs.Cell.CellDto
                {
                    Id = newCellSector.Cell.Id,
                    Name = newCellSector.Cell.Name,
                    Group = newCellSector.Cell.Group,
                    Description = newCellSector.Cell.Description ?? string.Empty,
                    Address = newCellSector.Cell.Address ?? string.Empty,
                    LAC = newCellSector.Cell.LAC,
                    TAC = newCellSector.Cell.TAC,
                    CID = newCellSector.Cell.CID,
                },
                AntennaType = newCellSector.AntennaType.ToString(),
                AntennaHeight = newCellSector.AntennaHeight,
                Azimuth = newCellSector.Azimuth,
                BeamTilt = newCellSector.BeamTilt
            },
            Message = "Cell sector created successfully.",
            Succeeded = true
        };
    }
}
