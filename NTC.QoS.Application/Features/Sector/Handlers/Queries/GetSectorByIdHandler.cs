using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Queries;

public class GetSectorByIdHandler(ISectorRepository sectorRepository) : IRequestHandler<GetSectorByIdRequest, ApiResponse<SectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;

    public async Task<ApiResponse<SectorDto>> Handle(GetSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var sectorQuery = _sectorRepository.Query(s => s.Id == request.Id);
        var sector = await _sectorRepository.FirstOrDefaultAsync(sectorQuery, cancellationToken);
        if (sector is null)
        {
            return new ApiResponse<SectorDto>
            {
                Message = "Sector does not exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullSector", ["Sector not found."]
                    }
                },
            };
        }

        var result = new SectorDto
        {
            Id = sector.Id,
            Network = sector.Network != null ? new()
            {
                Id = sector.Network.Id,
                Name = sector.Network.Name ?? string.Empty,
                SystemType = sector.Network.SystemType.ToString() ?? string.Empty,
            } : null,
            AntennaType = sector.AntennaType?.ToString() ?? AntennaTypeEnum.Unknown.ToString(),
            MIMO = sector.MIMO != null ? new()
            {
                Id = sector.MIMO.Id,
                Type = sector.MIMO.Type.ToString()
            } : null,
            CellSector = sector.CellSector != null ? new()
            {
                Id = sector.CellSector.Id,
                Cell = sector.CellSector.Cell != null ? new()
                {
                    Id = sector.CellSector.Cell.Id,
                    Name = sector.CellSector.Cell.Name ?? string.Empty,
                    Latitude = sector.CellSector.Cell.Location?.Coordinate.X ?? 0,
                    Longitude = sector.CellSector.Cell.Location?.Coordinate.Y ?? 0,
                    CID = sector.CellSector.Cell.CID ?? string.Empty,
                    LAC = sector.CellSector.Cell.LAC ?? string.Empty,
                } : null,
                AntennaType = sector.CellSector.AntennaType.ToString(),
                AntennaHeight = sector.CellSector.AntennaHeight,
                Azimuth = sector.CellSector.Azimuth,
                BeamTilt = sector.CellSector.BeamTilt,
            } : null,
            RadioEquipment = sector.RadioEquipment,
            IsRxEqualTx = sector.IsRxEqualTx,
            TxPower = sector.TxPower,
            TxAntennaModel = sector.TxAntennaModel,
            TxCableType = sector.TxCableType,
            TxCableLength = sector.TxCableLength,
            TxCableLoss = sector.TxCableLoss,
            TxAdditionalLoss = sector.TxAdditionalLoss,
            TxAntennaHeight = sector.TxAntennaHeight,
            TxAntennaGain = sector.TxAntennaGain,
            TxAzimuth = sector.TxAzimuth,
            TxBeamTilt = sector.TxBeamTilt,
            TxAntennaPattern = sector.TxAntennaPattern != null ? new()
            {
                Id = sector.TxAntennaPattern.Id,
                Name = sector.TxAntennaPattern.Name ?? string.Empty,
                URL = sector.TxAntennaPattern.URL ?? string.Empty,
            } : null,
            TxEIRP = sector.TxEIRP,
            RxAntennaModel = sector.RxAntennaModel,
            RxCableType = sector.RxCableType,
            RxCableLength = sector.RxCableLength,
            RxCableLoss = sector.RxCableLoss,
            RxAdditionalLoss = sector.RxAdditionalLoss,
            RxAntennaHeight = sector.RxAntennaHeight,
            RxAntennaGain = sector.RxAntennaGain,
            RxAzimuth = sector.RxAzimuth,
            RxBeamTilt = sector.RxBeamTilt,
            RxAntennaPattern = sector.RxAntennaPattern != null ? new()
            {
                Id = sector.RxAntennaPattern.Id,
                Name = sector.RxAntennaPattern.Name ?? string.Empty,
                URL = sector.RxAntennaPattern.URL ?? string.Empty,
            } : null,
            RxEIRP = sector.RxEIRP
        };

        return new ApiResponse<SectorDto>
        {
            Data = result,
            Message = "Sector retrieved successfully.",
            Succeeded = true
        };
    }
}
