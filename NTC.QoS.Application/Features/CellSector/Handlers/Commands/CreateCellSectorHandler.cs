using MediatR;
using NTC.QoS.Application.Features.CellSector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Handlers.Commands;

public class CreateCellSectorHandler(IRepository<Domain.Entities.CellSector> cellSectorRepository) : IRequestHandler<CreateCellSectorRequest, ApiResponse<CellSectorDto>>
{
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository = cellSectorRepository;

    public async Task<ApiResponse<CellSectorDto>> Handle(CreateCellSectorRequest request, CancellationToken cancellationToken)
    {
        var newCellSector = new Domain.Entities.CellSector
        {
            CellId = request.CellSector.CellId,
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
                CellId = newCellSector.CellId,
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
