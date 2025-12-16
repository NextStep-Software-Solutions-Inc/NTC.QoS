using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Commands;

public class UpdateSectorByIdHandler(ISectorRepository sectorRepository,
                                     INetworkRepository networkRepository,
                                     IMIMORepository mimoRepository,
                                     IRepository<Domain.Entities.CellSector> cellSectorRepository,
                                     IRepository<Domain.Entities.AntennaPattern> antennaPatternRepository) : IRequestHandler<UpdateSectorByIdRequest, ApiResponse<SectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    private readonly INetworkRepository _networkRepository = networkRepository;
    private readonly IMIMORepository _mimoRepository = mimoRepository;
    private readonly IRepository<Domain.Entities.CellSector> _cellSectorRepository = cellSectorRepository;
    private readonly IRepository<Domain.Entities.AntennaPattern> _antennaPatternRepository = antennaPatternRepository;

    public async Task<ApiResponse<SectorDto>> Handle(UpdateSectorByIdRequest request, CancellationToken cancellationToken)
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
        if (request.Sector.NetworkId.HasValue)
        {
            var networkQuery = _networkRepository.Query(n => n.Id == request.Sector.NetworkId.Value);
            var network = await _networkRepository.FirstOrDefaultAsync(networkQuery, cancellationToken);
            if (network is null)
            {
                return new ApiResponse<SectorDto>
                {
                    Message = "Network does not exists.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullNetwork", ["Network not found."]
                    }
                },
                };
            }
            sector.NetworkId = network.Id;
        }
        if (request.Sector.AntennaType is not null) sector.AntennaType = EnumHelper<AntennaTypeEnum>.ParseUserInput(request.Sector.AntennaType);
        if (request.Sector.MIMOId.HasValue)
        {
            var mimoQuery = _mimoRepository.Query(m => m.Id == request.Sector.MIMOId.Value);
            var mimo = await _mimoRepository.FirstOrDefaultAsync(mimoQuery, cancellationToken);
            if (mimo is null)
            {
                return new ApiResponse<SectorDto>
                {
                    Message = "MIMO does not exists.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        {
                            "NullMIMO", ["MIMO not found."]
                        }
                    },
                };
            }
            sector.MIMOId = mimo.Id;
        }
        if (request.Sector.CellSectorId.HasValue)
        {
            var cellSectorQuery = _cellSectorRepository.Query(c => c.Id == request.Sector.CellSectorId.Value);
            var cellSector = await _cellSectorRepository.FirstOrDefaultAsync(cellSectorQuery, cancellationToken);
            if (cellSector is null)
            {
                return new ApiResponse<SectorDto>
                {
                    Message = "Cell does not exists.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        {
                            "NullCell", ["Cell not found."]
                        }
                    },
                };
            }
            sector.CellSectorId = cellSector.Id;
        }
        if (!string.IsNullOrEmpty(request.Sector.RadioEquipment)) sector.RadioEquipment = request.Sector.RadioEquipment;
        if (request.Sector.IsRxEqualTx.HasValue) sector.IsRxEqualTx = request.Sector.IsRxEqualTx;
        if (request.Sector.TxPower.HasValue) sector.TxPower = request.Sector.TxPower;
        if (!string.IsNullOrEmpty(request.Sector.TxAntennaModel)) sector.TxAntennaModel = request.Sector.TxAntennaModel;
        if (!string.IsNullOrEmpty(request.Sector.TxCableType)) sector.TxCableType = request.Sector.TxCableType;
        if (request.Sector.TxCableLength.HasValue) sector.TxCableLength = request.Sector.TxCableLength;
        if (request.Sector.TxCableLoss.HasValue) sector.TxCableLoss = request.Sector.TxCableLoss;
        if (request.Sector.TxAdditionalLoss.HasValue) sector.TxAdditionalLoss = request.Sector.TxAdditionalLoss;
        if (request.Sector.TxAntennaHeight.HasValue) sector.TxAntennaHeight = request.Sector.TxAntennaHeight;
        if (request.Sector.TxAntennaGain.HasValue) sector.TxAntennaGain = request.Sector.TxAntennaGain;
        if (request.Sector.TxAzimuth.HasValue) sector.TxAzimuth = request.Sector.TxAzimuth;
        if (request.Sector.TxBeamTilt.HasValue) sector.TxBeamTilt = request.Sector.TxBeamTilt;
        if (request.Sector.TxAntennaPatternId.HasValue)
        {
            var txAntennaPatternQuery = _antennaPatternRepository.Query(a => a.Id == request.Sector.TxAntennaPatternId.Value);
            var txAntennaPattern = await _antennaPatternRepository.FirstOrDefaultAsync(txAntennaPatternQuery, cancellationToken);
            if (txAntennaPattern is null)
            {
                return new ApiResponse<SectorDto>
                {
                    Message = "Tx antenna pattern does not exists.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        {
                            "NullTxAntennaPattern", ["Tx antenna pattern not found."]
                        }
                    },
                };
            }
            sector.TxAntennaPatternId = txAntennaPattern.Id;
        }
        if (request.Sector.TxEIRP.HasValue) sector.TxEIRP = request.Sector.TxEIRP;
        if (request.Sector.IsRxEqualTx.HasValue && request.Sector.IsRxEqualTx.Value == true)
        {
            sector.RxAntennaModel = sector.TxAntennaModel;
            sector.RxCableType = sector.TxCableType;
            sector.RxCableLength = sector.TxCableLength;
            sector.RxCableLoss = sector.TxCableLoss;
            sector.RxAdditionalLoss = sector.TxAdditionalLoss;
            sector.RxAntennaHeight = sector.TxAntennaHeight;
            sector.RxAntennaGain = sector.TxAntennaGain;
            sector.RxAzimuth = sector.TxAzimuth;
            sector.RxBeamTilt = sector.TxBeamTilt;
            sector.RxAntennaPatternId = sector.TxAntennaPatternId;
            sector.RxEIRP = sector.TxEIRP;
        }
        else
        {
            if (!string.IsNullOrEmpty(request.Sector.RxAntennaModel)) sector.RxAntennaModel = request.Sector.RxAntennaModel;
            if (!string.IsNullOrEmpty(request.Sector.RxCableType)) sector.RxCableType = request.Sector.RxCableType;
            if (request.Sector.RxCableLength.HasValue) sector.RxCableLength = request.Sector.RxCableLength;
            if (request.Sector.RxCableLoss.HasValue) sector.RxCableLoss = request.Sector.RxCableLoss;
            if (request.Sector.RxAdditionalLoss.HasValue) sector.RxAdditionalLoss = request.Sector.RxAdditionalLoss;
            if (request.Sector.RxAntennaHeight.HasValue) sector.RxAntennaHeight = request.Sector.RxAntennaHeight;
            if (request.Sector.RxAntennaGain.HasValue) sector.RxAntennaGain = request.Sector.RxAntennaGain;
            if (request.Sector.RxAzimuth.HasValue) sector.RxAzimuth = request.Sector.RxAzimuth;
            if (request.Sector.RxBeamTilt.HasValue) sector.RxBeamTilt = request.Sector.RxBeamTilt;
            if (request.Sector.RxAntennaPatternId.HasValue)
            {
                var rxAntennaPatternQuery = _antennaPatternRepository.Query(a => a.Id == request.Sector.RxAntennaPatternId.Value);
                var rxAntennaPattern = await _antennaPatternRepository.FirstOrDefaultAsync(rxAntennaPatternQuery, cancellationToken);
                if (rxAntennaPattern is null)
                {
                    return new ApiResponse<SectorDto>
                    {
                        Message = "Rx antenna pattern does not exists.",
                        Succeeded = false,
                        Errors = new Dictionary<string, string[]>
                        {
                            {
                                "NullRxAntennaPattern", ["Rx antenna pattern not found."]
                            }
                        },
                    };
                }
                sector.RxAntennaPatternId = rxAntennaPattern.Id;
            }
            if (request.Sector.RxEIRP.HasValue) sector.RxEIRP = request.Sector.RxEIRP;
        }

        var existingSectorsQuery = _sectorRepository.Query(s => s.Id != sector.Id &&
                                    s.NetworkId == sector.NetworkId &&
                                    s.CellSectorId == sector.CellSectorId &&
                                    s.MIMOId == sector.MIMOId &&
                                    s.IsRxEqualTx == sector.IsRxEqualTx &&
                                    s.AntennaType == sector.AntennaType);
        var existingSectors = await _sectorRepository.FirstOrDefaultAsync(existingSectorsQuery, cancellationToken);
        if (existingSectors is not null)
        {
            return new ApiResponse<SectorDto>
            {
                Message = "A sector with the same Network, Number, Cell, and Antenna Type already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Sector already exists."]
                    }
                },
            };
        }

        _sectorRepository.Update(sector);
        await _sectorRepository.SaveChangesAsync(cancellationToken);

        var result = new SectorDto
        {
            Id = sector.Id,
            Network = sector.NetworkId.HasValue ? new()
            {
                Id = sector.NetworkId.Value,
                Name = sector.Network?.Name ?? string.Empty,
                SystemType = sector.Network?.SystemType.ToString() ?? string.Empty,
            } : null,
            AntennaType = sector.AntennaType?.ToString() ?? AntennaTypeEnum.Unknown.ToString(),
            MIMO = sector.MIMOId.HasValue ? new()
            {
                Id = sector.MIMOId.Value,
                Type = sector.MIMO?.Type.ToString() ?? string.Empty
            } : null,
            CellSector = new()
            {
                Id = sector.CellSector?.Id ?? Guid.Empty,
                Cell = new()
                {
                    Id = sector.CellSector?.Cell?.Id ?? Guid.Empty,
                    Name = sector.CellSector?.Cell?.Name ?? string.Empty,
                    Latitude = sector.CellSector?.Cell?.Location.Coordinate.X ?? 0,
                    Longitude = sector.CellSector?.Cell?.Location.Coordinate.Y ?? 0,
                    CID = sector.CellSector?.Cell?.CID ?? string.Empty,
                    LAC = sector.CellSector?.Cell?.LAC ?? string.Empty,
                },
                AntennaType = sector.CellSector?.AntennaType.ToString() ?? AntennaTypeEnum.Unknown.ToString(),
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
            TxAntennaPattern = sector.TxAntennaPatternId.HasValue ? new()
            {
                Id = sector.TxAntennaPattern?.Id ?? Guid.Empty,
                Name = sector.TxAntennaPattern?.Name ?? string.Empty,
                URL = sector.TxAntennaPattern?.URL ?? string.Empty,
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
            RxAntennaPattern = sector.RxAntennaPatternId.HasValue ? new()
            {
                Id = sector.RxAntennaPattern?.Id ?? Guid.Empty,
                Name = sector.RxAntennaPattern?.Name ?? string.Empty,
                URL = sector.RxAntennaPattern?.URL ?? string.Empty,
            } : null,
            RxEIRP = sector.RxEIRP
        };

        return new ApiResponse<SectorDto>
        {
            Data = result,
            Message = "Sector updated successfully.",
            Succeeded = true
        };
    }
}
