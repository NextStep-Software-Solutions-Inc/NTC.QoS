using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
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
                Network = new()
                {
                    Id = s.Network.Id,
                    Name = s.Network.Name,
                    SystemType = s.Network.SystemType.ToString()!,
                },
                AntennaType = s.AntennaType.ToString(),
                MIMO = s.MIMO != null ? new()
                {
                    Id = s.MIMO.Id,
                    Type = s.MIMO.Type.ToString()
                } : null,
                CellSector = new()
                {
                    Id = s.CellSector.Id,
                    Cell = new()
                    {
                        Id = s.CellSector.Cell.Id,
                        Name = s.CellSector.Cell.Name,
                        Latitude = s.CellSector.Cell.Location.Coordinate.X,
                        Longitude = s.CellSector.Cell.Location.Coordinate.Y,
                        CID = s.CellSector.Cell.CID,
                        LAC = s.CellSector.Cell.LAC,
                    }
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
