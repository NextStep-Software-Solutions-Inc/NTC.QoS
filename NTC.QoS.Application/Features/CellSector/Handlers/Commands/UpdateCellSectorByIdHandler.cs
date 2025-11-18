using MediatR;
using NTC.QoS.Application.Features.CellSector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Handlers.Commands;

public class UpdateCellSectorByIdHandler(IRepository<Domain.Entities.CellSector> cellSectorRepository) : IRequestHandler<UpdateCellSectorByIdRequest, ApiResponse<UpdateCellSectorDto>>
{
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository = cellSectorRepository;

    public async Task<ApiResponse<UpdateCellSectorDto>> Handle(UpdateCellSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var cellSectorQuery = _cellSectorRepository.Query(cs => !cs.IsDeleted && cs.Id.Equals(request.Id));
        var cellSector = await _cellSectorRepository.FirstOrDefaultAsync(cellSectorQuery, cancellationToken);
        if (cellSector is null)
        {
            return new ApiResponse<UpdateCellSectorDto>
            {
                Message = "Cell sector not found",
                Succeeded = false,
            };
        }

        cellSector.AntennaType = EnumHelper<AntennaTypeEnum>.ParseUserInput(request.CellSector.AntennaType ?? cellSector.AntennaType.ToString());
        cellSector.AntennaHeight = request.CellSector.AntennaHeight ?? cellSector.AntennaHeight;
        cellSector.Azimuth = request.CellSector.Azimuth ?? cellSector.Azimuth;
        cellSector.BeamTilt = request.CellSector.BeamTilt ?? cellSector.BeamTilt;

        _cellSectorRepository.Update(cellSector);
        await _cellSectorRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UpdateCellSectorDto>
        {
            Data = new()
            {
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
