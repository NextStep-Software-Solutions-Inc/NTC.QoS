using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Queries;

public class GetAllSectorsHandler(ISectorRepository sectorRepository) : IRequestHandler<GetAllSectorsRequest, PaginatedResponse<SectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    public async Task<PaginatedResponse<SectorDto>> Handle(GetAllSectorsRequest request, CancellationToken cancellationToken)
    {
        var allSectorsQuery = _sectorRepository.Query(s => !s.IsDeleted);

        allSectorsQuery = allSectorsQuery
            .Where(s => !request.NetworkId.HasValue || s.NetworkId == request.NetworkId)
            .Where(s => !request.MIMOId.HasValue || s.MIMOId == request.MIMOId)
            .Where(s => !request.CellSectorId.HasValue || s.CellSectorId == request.CellSectorId)
            .Where(s => !request.TxAntennaPatternId.HasValue || s.TxAntennaPatternId == request.TxAntennaPatternId)
            .Where(s => !request.RxAntennaPatternId.HasValue || s.RxAntennaPatternId == request.RxAntennaPatternId);

        var allSectorDtosQuery = allSectorsQuery
            .OrderBy(s => s.CreatedAt)
            .Select(s => new SectorDto
            {
                Id = s.Id,
                Network = new()
                {
                    Id = s.Network.Id,
                    Name = s.Network.Name,
                    SystemType = s.Network.SystemType.ToString()!,
                },
                AntennaType = EnumHelper<AntennaTypeEnum>.GetDescription(s.AntennaType),
                MIMO = s.MIMO != null ? new()
                {
                    Id = s.MIMO.Id,
                    Type = s.MIMO.Type.ToString()
                } : null,
                CellSector = new CellSectorDto
                {
                    Id = s.CellSector != null ? s.CellSector.Id : Guid.Empty,
                    Cell = new()
                    {
                        Id = s.CellSector != null && s.CellSector.Cell != null ? s.CellSector.Cell.Id : Guid.Empty,
                        Name = s.CellSector != null && s.CellSector.Cell != null ? s.CellSector.Cell.Name : string.Empty,
                        Latitude = s.CellSector != null && s.CellSector.Cell != null ? s.CellSector.Cell.Location.Coordinate.X : 0,
                        Longitude = s.CellSector != null && s.CellSector.Cell != null ? s.CellSector.Cell.Location.Coordinate.Y : 0,
                        CID = s.CellSector != null && s.CellSector.Cell != null ? s.CellSector.Cell.CID : string.Empty,
                        LAC = s.CellSector != null && s.CellSector.Cell != null ? s.CellSector.Cell.LAC : string.Empty,
                    },
                    AntennaType = EnumHelper<AntennaTypeEnum>.GetDescription(s.CellSector != null ? s.CellSector.AntennaType : AntennaTypeEnum.Unknown),
                    AntennaHeight = s.CellSector != null ? s.CellSector.AntennaHeight : 0,
                    Azimuth = s.CellSector != null ? s.CellSector.Azimuth : 0,
                    BeamTilt = s.CellSector != null ? s.CellSector.BeamTilt : 0,
                },
                RadioEquipment = s.RadioEquipment,
                IsRxEqualTx = s.IsRxEqualTx,
                TxPower = s.TxPower,
                TxAntennaModel = s.TxAntennaModel,
                TxCableType = s.TxCableType,
                TxCableLength = s.TxCableLength,
                TxCableLoss = s.TxCableLoss,
                TxAdditionalLoss = s.TxAdditionalLoss,
                TxAntennaHeight = s.TxAntennaHeight,
                TxAntennaGain = s.TxAntennaGain,
                TxAzimuth = s.TxAzimuth,
                TxBeamTilt = s.TxBeamTilt,
                TxAntennaPattern = new()
                {
                    Id = s.TxAntennaPatternId,
                    Name = s.TxAntennaPattern.Name,
                    URL = s.TxAntennaPattern.URL,
                },
                TxEIRP = s.TxEIRP,
                RxAntennaModel = s.RxAntennaModel,
                RxCableType = s.RxCableType,
                RxCableLength = s.RxCableLength,
                RxCableLoss = s.RxCableLoss,
                RxAdditionalLoss = s.RxAdditionalLoss,
                RxAntennaHeight = s.RxAntennaHeight,
                RxAntennaGain = s.RxAntennaGain,
                RxAzimuth = s.RxAzimuth,
                RxBeamTilt = s.RxBeamTilt,
                RxAntennaPattern = new()
                {
                    Id = s.RxAntennaPatternId,
                    Name = s.RxAntennaPattern.Name,
                    URL = s.RxAntennaPattern.URL,
                },
                RxEIRP = s.RxEIRP
            });

        return await _sectorRepository.PaginateAsync(allSectorDtosQuery, request.PageNumber, request.PageSize, cancellationToken);

    }
}
