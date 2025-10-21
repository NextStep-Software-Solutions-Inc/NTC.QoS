using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Commands;

public class CreateSectorHandler(
    ISectorRepository sectorRepository,
    INetworkRepository networkRepository,
    IMIMORepository mimoRepository,
    ICellSectorRepository cellSectorRepository,
    IRepository<Domain.Entities.AntennaPattern> antennaPatternRepository) : IRequestHandler<CreateSectorRequest, ApiResponse<SectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    private readonly INetworkRepository _networkRepository = networkRepository;
    private readonly IMIMORepository _mimoRepository = mimoRepository;
    private readonly ICellSectorRepository _cellSectorRepository = cellSectorRepository;
    private readonly IRepository<Domain.Entities.AntennaPattern> _antennaPatternRepository = antennaPatternRepository;

    public async Task<ApiResponse<SectorDto>> Handle(CreateSectorRequest request, CancellationToken cancellationToken)
    {
        var sectorQuery = _sectorRepository.Query(s => s.NetworkId == request.Sector.NetworkId
                                                    && s.CellSectorId == request.Sector.CellSectorId
                                                    && s.MIMOId == request.Sector.MIMOId
                                                    && s.IsRxEqualTx == request.Sector.IsRxEqualTx);
        var sector = await _sectorRepository.FirstOrDefaultAsync(sectorQuery, cancellationToken);
        if (sector is not null)
        {
            return new ApiResponse<SectorDto>
            {
                Message = "Sector already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Sector already exists."] 
                    }
                },
            };
        }

        // Validate NetworkId if provided
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
        }

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
        }

        var cellSectorQuery = _cellSectorRepository.Query(cs => cs.Id == request.Sector.CellSectorId);
        var cellSector = await _cellSectorRepository.FirstOrDefaultAsync(cellSectorQuery, cancellationToken);
        if (cellSector is null)
        {
            return new ApiResponse<SectorDto>
            {
                Message = "Cell Sector does not exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullCellSector", ["Cell Sector not found."]
                    }
                },
            };
        }

        // Validate TxAntennaPatternId if provided
        if (request.Sector.TxAntennaPatternId.HasValue)
        {
            var txAntennaPatternQuery = _antennaPatternRepository.Query(tap => tap.Id == request.Sector.TxAntennaPatternId.Value);
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
        }

        var newSector = CreateSectorEntity(request.Sector, cellSector);

        if (request.Sector.IsRxEqualTx == true)
        {
            CopyTxToRx(newSector);
        }
        else
        {
            // Validate RxAntennaPatternId if provided and IsRxEqualTx is false
            if (request.Sector.RxAntennaPatternId.HasValue)
            {
                var rxAntennaPatternQuery = _antennaPatternRepository.Query(rap => rap.Id == request.Sector.RxAntennaPatternId.Value);
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
            }
            AssignRxProperties(newSector, request.Sector);
        }

        _sectorRepository.Add(newSector);
        await _sectorRepository.SaveChangesAsync(cancellationToken);

        var sectorWithIncludes = await _sectorRepository.FirstOrDefaultAsync(
            _sectorRepository.Query(s => s.Id == newSector.Id),
            cancellationToken);

        return CreateSectorApiResponse(sectorWithIncludes!);
    }

    private static Domain.Entities.Sector CreateSectorEntity(CreateSectorDto dto, Domain.Entities.CellSector cellSector) => new()
    {
        NetworkId = dto.NetworkId,
        MIMOId = dto.MIMOId,
        CellSectorId = cellSector.Id,
        AntennaType = cellSector.AntennaType,
        RadioEquipment = dto.RadioEquipment,
        IsRxEqualTx = dto.IsRxEqualTx,
        TxPower = dto.TxPower,
        TxAntennaModel = dto.TxAntennaModel,
        TxCableType = dto.TxCableType,
        TxCableLength = dto.TxCableLength,
        TxCableLoss = dto.TxCableLoss,
        TxAdditionalLoss = dto.TxAdditionalLoss,
        TxAntennaHeight = cellSector.AntennaHeight,
        TxAntennaGain = dto.TxAntennaGain,
        TxAzimuth = cellSector.Azimuth,
        TxBeamTilt = cellSector.BeamTilt,
        TxAntennaPatternId = dto.TxAntennaPatternId,
        TxEIRP = dto.TxEIRP,
    };

    private static void CopyTxToRx(Domain.Entities.Sector sector)
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

    private static void AssignRxProperties(Domain.Entities.Sector sector, CreateSectorDto dto)
    {
        sector.RxAntennaModel = dto.RxAntennaModel;
        sector.RxCableType = dto.RxCableType;
        sector.RxCableLength = dto.RxCableLength;
        sector.RxCableLoss = dto.RxCableLoss;
        sector.RxAdditionalLoss = dto.RxAdditionalLoss;
        sector.RxAntennaHeight = dto.RxAntennaHeight;
        sector.RxAntennaGain = dto.RxAntennaGain;
        sector.RxAzimuth = dto.RxAzimuth;
        sector.RxBeamTilt = dto.RxBeamTilt;
        sector.RxAntennaPatternId = dto.RxAntennaPatternId;
        sector.RxEIRP = dto.RxEIRP;
    }

    private static ApiResponse<SectorDto> CreateSectorApiResponse(Domain.Entities.Sector newSector)
    {
        return new()
        {
            Data = new SectorDto
            {
                Id = newSector.Id,
                Network = newSector.NetworkId.HasValue ? new()
                {
                    Id = newSector.NetworkId.Value,
                    Name = newSector.Network?.Name ?? string.Empty,
                    SystemType = newSector.Network?.SystemType.ToString() ?? string.Empty,
                } : null,
                AntennaType = newSector.AntennaType?.ToString() ?? AntennaTypeEnum.Unknown.ToString(),
                MIMO = newSector.MIMO != null ? new()
                {
                    Id = newSector.MIMO.Id,
                    Type = newSector.MIMO.Type.ToString()
                } : null,
                CellSector = new()
                {
                    Id = newSector.CellSector?.Id ?? Guid.Empty,
                    Cell = new ()
                    {
                        Id = newSector.CellSector?.Cell?.Id ?? Guid.Empty,
                        Name = newSector.CellSector?.Cell?.Name ?? string.Empty,
                        Latitude = newSector.CellSector?.Cell.Location.Coordinate.X ?? 0,
                        Longitude = newSector.CellSector?.Cell?.Location.Coordinate.Y ?? 0,
                        CID = newSector.CellSector?.Cell?.CID ?? string.Empty,
                        LAC = newSector.CellSector?.Cell?.LAC ?? string.Empty,
                    },
                    AntennaType = newSector.CellSector?.AntennaType.ToString() ?? AntennaTypeEnum.Unknown.ToString(),
                    AntennaHeight = newSector.CellSector?.AntennaHeight ?? 0,
                    Azimuth = newSector.CellSector?.Azimuth ?? 0,
                    BeamTilt = newSector.CellSector?.BeamTilt ?? 0,
                },
                RadioEquipment = newSector.RadioEquipment,
                IsRxEqualTx = newSector.IsRxEqualTx,
                TxPower = newSector.TxPower,
                TxAntennaModel = newSector.TxAntennaModel,
                TxCableType = newSector.TxCableType,
                TxCableLength = newSector.TxCableLength,
                TxCableLoss = newSector.TxCableLoss,
                TxAdditionalLoss = newSector.TxAdditionalLoss,
                TxAntennaHeight = newSector.TxAntennaHeight,
                TxAntennaGain = newSector.TxAntennaGain,
                TxAzimuth = newSector.TxAzimuth,
                TxBeamTilt = newSector.TxBeamTilt,
                TxAntennaPattern = newSector.TxAntennaPatternId.HasValue ? new()
                {
                    Id = newSector.TxAntennaPattern?.Id ?? Guid.Empty,
                    Name = newSector.TxAntennaPattern?.Name ?? string.Empty,
                    URL = newSector.TxAntennaPattern?.URL ?? string.Empty,
                } : null,
                TxEIRP = newSector.TxEIRP,
                RxAntennaModel = newSector.RxAntennaModel,
                RxCableType = newSector.RxCableType,
                RxCableLength = newSector.RxCableLength,
                RxCableLoss = newSector.RxCableLoss,
                RxAdditionalLoss = newSector.RxAdditionalLoss,
                RxAntennaHeight = newSector.RxAntennaHeight,
                RxAntennaGain = newSector.RxAntennaGain,
                RxAzimuth = newSector.RxAzimuth,
                RxBeamTilt = newSector.RxBeamTilt,
                RxAntennaPattern = newSector.RxAntennaPatternId.HasValue ? new()
                {
                    Id = newSector.RxAntennaPattern?.Id ?? Guid.Empty,
                    Name = newSector.RxAntennaPattern?.Name ?? string.Empty,
                    URL = newSector.RxAntennaPattern?.URL ?? string.Empty,
                } : null,
                RxEIRP = newSector.RxEIRP,
            },
            Succeeded = true,
            Message = "Sector created successfully."
        };
    }
}
