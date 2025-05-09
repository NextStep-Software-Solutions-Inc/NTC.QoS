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
                                     ICellRepository cellRepository,
                                     IRepository<Domain.Entities.AntennaPattern> antennaPatternRepository) : IRequestHandler<UpdateSectorByIdRequest, ApiResponse<SectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    private readonly INetworkRepository _networkRepository = networkRepository;
    private readonly IMIMORepository _mimoRepository = mimoRepository;
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IRepository<Domain.Entities.AntennaPattern> _antennaPatternRepository = antennaPatternRepository;

    public async Task<ApiResponse<SectorDto>> Handle(UpdateSectorByIdRequest request, CancellationToken cancellationToken)
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
        if (request.Sector.NetworkId.HasValue)
        {
            var networkQuery = _networkRepository.Query(n => !n.IsDeleted && n.Id == request.Sector.NetworkId.Value);
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
                        "Invalid Id", ["Network not found."]
                    }
                },
                };
            }
            sector.NetworkId = network.Id;
        }
        if (request.Sector.AntennaType is not null) sector.AntennaType = EnumHelper<AntennaTypeEnum>.ParseUserInput(request.Sector.AntennaType);
        if (request.Sector.Number.HasValue) sector.Number = request.Sector.Number.Value;
        if (request.Sector.MIMOId.HasValue)
        {
            var mimo = await _mimoRepository.GetByIdAsync(request.Sector.MIMOId.Value, cancellationToken);
            if (mimo is null)
            {
                return new ApiResponse<SectorDto>
                {
                    Message = "MIMO does not exists.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        {
                            "Invalid Id", ["MIMO not found."]
                        }
                    },
                };
            }
            sector.MIMOId = mimo.Id;
        }
        if (request.Sector.CellSectorId.HasValue)
        {
            var cellSectorQuery = _sectorRepository.Query(c => !c.IsDeleted && c.Id == request.Sector.CellSectorId.Value);
            var cellSector = await _cellRepository.FirstOrDefaultAsync(cellSectorQuery, cancellationToken);
            if (cellSector is null)
            {
                return new ApiResponse<SectorDto>
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
            sector.CellSectorId = cellSector.Id;
        }
        if (!string.IsNullOrEmpty(request.Sector.RadioEquipment)) sector.RadioEquipment = request.Sector.RadioEquipment;
        if (request.Sector.IsRxEqualTx.HasValue) sector.IsRxEqualTx = request.Sector.IsRxEqualTx.Value;
        if (request.Sector.TxPower.HasValue) sector.TxPower = request.Sector.TxPower.Value;
        if (!string.IsNullOrEmpty(request.Sector.TxAntennaModel)) sector.TxAntennaModel = request.Sector.TxAntennaModel;
        if (!string.IsNullOrEmpty(request.Sector.TxCableType)) sector.TxCableType = request.Sector.TxCableType;
        if (request.Sector.TxCableLength.HasValue) sector.TxCableLength = request.Sector.TxCableLength.Value;
        if (request.Sector.TxCableLoss.HasValue) sector.TxCableLoss = request.Sector.TxCableLoss.Value;
        if (request.Sector.TxAdditionalLoss.HasValue) sector.TxAdditionalLoss = request.Sector.TxAdditionalLoss.Value;
        if (request.Sector.TxAntennaHeight.HasValue) sector.TxAntennaHeight = request.Sector.TxAntennaHeight.Value;
        if (request.Sector.TxAntennaGain.HasValue) sector.TxAntennaGain = request.Sector.TxAntennaGain.Value;
        if (request.Sector.TxAzimuth.HasValue) sector.TxAzimuth = request.Sector.TxAzimuth.Value;
        if (request.Sector.TxBeamTilt.HasValue) sector.TxBeamTilt = request.Sector.TxBeamTilt.Value;
        if (request.Sector.TxAntennaPatternId.HasValue) 
        {
            var txAntennaPattern = await _antennaPatternRepository.GetByIdAsync(request.Sector.TxAntennaPatternId.Value, cancellationToken);
            if (txAntennaPattern is null)
            {
                return new ApiResponse<SectorDto>
                {
                    Message = "Tx antenna pattern does not exists.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        {
                            "Invalid Id", ["Tx antenna pattern not found."]
                        }
                    },
                };
            }
            sector.TxAntennaPatternId = txAntennaPattern.Id;
        }
        if (request.Sector.TxEIRP.HasValue) sector.TxEIRP = request.Sector.TxEIRP.Value;
        if (request.Sector.IsRxEqualTx.HasValue && request.Sector.IsRxEqualTx.Value)
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
            if (request.Sector.RxCableLength.HasValue) sector.RxCableLength = request.Sector.RxCableLength.Value;
            if (request.Sector.RxCableLoss.HasValue) sector.RxCableLoss = request.Sector.RxCableLoss.Value;
            if (request.Sector.RxAdditionalLoss.HasValue) sector.RxAdditionalLoss = request.Sector.RxAdditionalLoss.Value;
            if (request.Sector.RxAntennaHeight.HasValue) sector.RxAntennaHeight = request.Sector.RxAntennaHeight.Value;
            if (request.Sector.RxAntennaGain.HasValue) sector.RxAntennaGain = request.Sector.RxAntennaGain.Value;
            if (request.Sector.RxAzimuth.HasValue) sector.RxAzimuth = request.Sector.RxAzimuth.Value;
            if (request.Sector.RxBeamTilt.HasValue) sector.RxBeamTilt = request.Sector.RxBeamTilt.Value;
            if (request.Sector.RxAntennaPatternId.HasValue)
            {
                var txAntennaPatternQuery = _sectorRepository.Query(a => !a.IsDeleted && a.Id == request.Sector.RxAntennaPatternId.Value);
                var rxAntennaPattern = await _antennaPatternRepository.FirstOrDefaultAsync(txAntennaPatternQuery, cancellationToken);
                if (rxAntennaPattern is null)
                {
                    return new ApiResponse<SectorDto>
                    {
                        Message = "Rx antenna pattern does not exists.",
                        Succeeded = false,
                        Errors = new Dictionary<string, string[]>
                        {
                            {
                                "Invalid Id", ["Rx antenna pattern not found."]
                            }
                        },
                    };
                }
                sector.RxAntennaPatternId = rxAntennaPattern.Id;
            }
            if (request.Sector.RxEIRP.HasValue) sector.RxEIRP = request.Sector.RxEIRP.Value;
        }

        var existingSectors = await _sectorRepository.GetAllSectorsAsync(cancellationToken);
        if (existingSectors.Any(s => s.Id != sector.Id &&
                                    s.NetworkId == sector.NetworkId &&
                                    s.Number == sector.Number &&
                                    s.CellSectorId == sector.CellSectorId &&
                                    s.MIMOId == sector.MIMOId &&
                                    s.IsRxEqualTx == sector.IsRxEqualTx &&
                                    s.AntennaType == sector.AntennaType))
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
            Network = new NetworkSectorDto
            {
                Name = sector.Network?.Name ?? string.Empty,
                SystemType = sector.Network?.SystemType.ToString() ?? string.Empty,
            },
            AntennaType = EnumHelper<AntennaTypeEnum>.GetDescription(sector.AntennaType),
            Number = sector.Number,
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
                    CoorLatitude = sector.CellSector?.Cell?.CoorLatitude ?? string.Empty,
                    CoorLongitude = sector.CellSector?.Cell?.CoorLongitude ?? string.Empty,
                    CID = sector.CellSector?.Cell?.CID ?? string.Empty,
                    LAC = sector.CellSector?.Cell?.LAC ?? string.Empty,
                },
                AntennaType = EnumHelper<AntennaTypeEnum>.GetDescription(sector.CellSector?.AntennaType ?? AntennaTypeEnum.Unknown),
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
            Message = "Sector updated successfully.",
            Succeeded = true
        };
    }
}
