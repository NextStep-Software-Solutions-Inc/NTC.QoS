using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.CellSector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Handlers.Commands;

public class UpdateCellSectorByIdHandler(IRepository<Domain.Entities.CellSector> cellSectorRepository) : IRequestHandler<UpdateCellSectorByIdRequest, ApiResponse<CellSectorDto>>
{
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository = cellSectorRepository;

    public async Task<ApiResponse<CellSectorDto>> Handle(UpdateCellSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var cellSectorQuery = _cellSectorRepository.Query(cs => !cs.IsDeleted && cs.Id.Equals(request.Id));
        var cellSector = await _cellSectorRepository.FirstOrDefaultAsync(cellSectorQuery, cancellationToken);
        if (cellSector is null)
        {
            return new ApiResponse<CellSectorDto>
            {
                Message = "Cell sector not found",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullCellSector", ["Cell sector not found."]
                    }
                }
            };
        }

        if (request.CellSector.CellId.HasValue) cellSector.CellId = request.CellSector.CellId.Value;
        if(!string.IsNullOrEmpty(request.CellSector.AntennaType)) cellSector.AntennaType = EnumHelper<AntennaTypeEnum>.ParseUserInput(request.CellSector.AntennaType);
        if (request.CellSector.AntennaHeight.HasValue && request.CellSector.AntennaHeight.Value != 0)
        {
            cellSector.AntennaHeight = request.CellSector.AntennaHeight.Value;
        }
        if (request.CellSector.Azimuth.HasValue && request.CellSector.Azimuth.Value != 0)
        {
            cellSector.Azimuth = request.CellSector.Azimuth.Value;
        }
        if (request.CellSector.BeamTilt.HasValue && request.CellSector.BeamTilt.Value != 0)
        {
            cellSector.BeamTilt = request.CellSector.BeamTilt.Value;
        }

        _cellSectorRepository.Update(cellSector);
        await _cellSectorRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CellSectorDto>
        {
            Data = new CellSectorDto
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
                AntennaType = cellSector.AntennaType.ToString(),
                AntennaHeight = cellSector.AntennaHeight,
                Azimuth = cellSector.Azimuth,
                BeamTilt = cellSector.BeamTilt
            },
            Succeeded = true,
            Message = "Cell sector updated successfully."
        };
    }
}
