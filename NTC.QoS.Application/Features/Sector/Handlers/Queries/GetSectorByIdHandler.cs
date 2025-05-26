using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Queries;

public class GetSectorByIdHandler(ISectorRepository sectorRepository) : IRequestHandler<GetSectorByIdRequest, ApiResponse<SectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    public async Task<ApiResponse<SectorDto>> Handle(GetSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var sectorQuery = _sectorRepository.Query(s => !s.IsDeleted && s.Id == request.Id);
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
                        "Invalid Id", ["Sector not found."]
                    }
                },
            };
        }

        var result = new SectorDto
        {
            Id = sector.Id,
            Network = new NetworkSectorDto
            {
                Name = sector.Network?.Name ?? string.Empty,
                SystemType = sector.Network?.SystemType.ToString() ?? string.Empty,
            },
            AntennaType = EnumHelper<AntennaTypeEnum>.GetDescription(sector.AntennaType),
            MIMO = new MIMOSectorDto
            {
                Id = sector.MIMOId ?? Guid.Empty,
                Type = sector.MIMO?.Type.ToString() ?? string.Empty
            },
            CellSector = new CellSectorDto
            {
                Id = sector.CellSector?.Id ?? Guid.Empty,
                Cell = new()
                {
                    Id = sector.CellSector?.Cell?.Id ?? Guid.Empty,
                    Name = sector.CellSector?.Cell?.Name ?? string.Empty,
                    Latitude = sector.CellSector?.Cell?.Location.X ?? 0,
                    Longitude = sector.CellSector?.Cell?.Location.Y ?? 0,
                    CID = sector.CellSector?.Cell?.CID ?? string.Empty,
                    LAC = sector.CellSector?.Cell?.LAC ?? string.Empty,
                },
                AntennaType = EnumHelper<AntennaTypeEnum>.GetDescription(sector.CellSector?.AntennaType ?? AntennaTypeEnum.Unknown),
                AntennaHeight = sector.CellSector?.AntennaHeight ?? 0,
                Azimuth = sector.CellSector?.Azimuth ?? 0,
                BeamTilt = sector.CellSector?.BeamTilt ?? 0,
            },
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
            TxAntennaPattern = new AntennaPatternSectorDto
            {
                Id = sector.TxAntennaPatternId,
                Name = sector.TxAntennaPattern?.Name ?? string.Empty,
                URL = sector.TxAntennaPattern?.URL ?? string.Empty,
            },
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
            RxAntennaPattern = new AntennaPatternSectorDto
            {
                Id = sector.RxAntennaPatternId,
                Name = sector.RxAntennaPattern?.Name ?? string.Empty,
                URL = sector.RxAntennaPattern?.URL ?? string.Empty,
            },
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
