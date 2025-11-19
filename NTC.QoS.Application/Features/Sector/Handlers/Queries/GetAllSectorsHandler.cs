using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Queries;

public class GetAllSectorsHandler(ISectorRepository sectorRepository) : IRequestHandler<GetAllSectorsRequest, PaginatedResponse<SectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;

    public async Task<PaginatedResponse<SectorDto>> Handle(GetAllSectorsRequest request, CancellationToken cancellationToken)
    {
        var allSectorsQuery = _sectorRepository.Query();

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
                Network = s.Network != null ? new()
                {
                    Id = s.Network.Id,
                    Name = s.Network.Name ?? string.Empty,
                    SystemType = s.Network.SystemType.ToString()!,
                } : null,
                AntennaType = s.AntennaType != null ? s.AntennaType.ToString()! : AntennaTypeEnum.Unknown.ToString(),
                MIMO = s.MIMO != null ? new()
                {
                    Id = s.MIMO.Id,
                    Type = s.MIMO.Type.ToString()
                } : null,
                CellSector = s.CellSector != null ? new()
                {
                    Id = s.CellSector.Id,
                    Cell = s.CellSector.Cell != null ? new()
                    {
                        Id = s.CellSector.Cell.Id,
                        Name = s.CellSector.Cell.Name ?? string.Empty,
                        Latitude = s.CellSector.Cell.Location != null ? s.CellSector.Cell.Location.Coordinate.X : 0,
                        Longitude = s.CellSector.Cell.Location != null ? s.CellSector.Cell.Location.Coordinate.Y : 0,
                        CID = s.CellSector.Cell.CID ?? string.Empty,
                        LAC = s.CellSector.Cell.LAC ?? string.Empty,
                    } : null
                } : null,
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
                TxAntennaPattern = s.TxAntennaPattern != null ? new()
                {
                    Id = s.TxAntennaPattern.Id,
                    Name = s.TxAntennaPattern.Name ?? string.Empty,
                    URL = s.TxAntennaPattern.URL ?? string.Empty,
                } : null,
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
                RxAntennaPattern = s.RxAntennaPattern != null ? new()
                {
                    Id = s.RxAntennaPattern.Id,
                    Name = s.RxAntennaPattern.Name ?? string.Empty,
                    URL = s.RxAntennaPattern.URL ?? string.Empty,
                } : null,
                RxEIRP = s.RxEIRP
            });

        return await _sectorRepository.PaginateAsync(allSectorDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
