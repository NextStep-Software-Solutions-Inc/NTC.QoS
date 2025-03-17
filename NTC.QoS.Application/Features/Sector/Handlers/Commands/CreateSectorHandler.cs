using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Commands;

public class CreateSectorHandler(IRepository<Domain.Entities.Sector> sectorRepository, INetworkRepository networkRepository, IMIMORepository mimoRepository, ICellRepository cellRepository, IRepository<Domain.Entities.AntennaPattern> antennaPatternRepository) : IRequestHandler<CreateSectorRequest, ApiResponse<CreateSectorDto>>
{
    private readonly IRepository<Domain.Entities.Sector> _sectorRepository = sectorRepository;
    private readonly INetworkRepository _networkRepository = networkRepository;
    private readonly IMIMORepository _mimoRepository = mimoRepository;
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IRepository<Domain.Entities.AntennaPattern> _antennaPatternRepository = antennaPatternRepository;
    public async Task<ApiResponse<CreateSectorDto>> Handle(CreateSectorRequest request, CancellationToken cancellationToken)
    {
        var antennaType = EnumHelper<AntennaTypeEnum>.ParseUserInput(request.Sector.AntennaType);

        var sector = await _sectorRepository.FindAsync(s => s.NetworkId == request.Sector.NetworkId && s.CellId == request.Sector.CellId && s.AntennaType == antennaType && s.Number == request.Sector.Number && !s.IsDeleted, cancellationToken);
        if (sector is not null)
        {
            return new ApiResponse<CreateSectorDto>
            {
                Message = "Sector already exist.",
                Succeeded = false,
            };
        }

        var network = await _networkRepository.GetByIdAsync(request.Sector.NetworkId, cancellationToken);
        if (network is null)
        {
            return new ApiResponse<CreateSectorDto>
            {
                Message = "Network not found.",
                Succeeded = false
            };
        }

        if (request.Sector.MIMOId.HasValue)
        {
            var mimo = await _mimoRepository.GetByIdAsync(request.Sector.MIMOId.Value, cancellationToken);
            if (mimo is null)
            {
                return new ApiResponse<CreateSectorDto>
                {
                    Message = "MIMO not found.",
                    Succeeded = false
                };
            }
        }

        var cell = await _cellRepository.GetByIdAsync(request.Sector.CellId, cancellationToken);
        if (cell is null) 
        {
            return new ApiResponse<CreateSectorDto>
            {
                Message = "Cell not found.",
                Succeeded = false
            };
        }

        var txAntennaPattern = await _antennaPatternRepository.GetByIdAsync(request.Sector.TxAntennaPatternId, cancellationToken);
        var rxAntennaPattern = await _antennaPatternRepository.GetByIdAsync(request.Sector.RxAntennaPatternId, cancellationToken);
        if (txAntennaPattern is null || rxAntennaPattern is null)
        {
            return new ApiResponse<CreateSectorDto>
            {
                Message = "Antenna pattern not found.",
                Succeeded = false
            };
        }

        var newSector = new Domain.Entities.Sector  
        {
            NetworkId = network.Id,
            MIMOId = request.Sector.MIMOId,
            CellId = cell.Id,
            AntennaType = antennaType,
            Number = request.Sector.Number,
            RadioEquipment = request.Sector.RadioEquipment,
            IsRxEqualTx = request.Sector.IsRxEqualTx,
            TxPower = request.Sector.TxPower,
            TxAntennaModel = request.Sector.TxAntennaModel,
            TxCableType = request.Sector.TxCableType,
            TxCableLength = request.Sector.TxCableLength,
            TxCableLoss = request.Sector.TxCableLoss,
            TxAdditionalLoss = request.Sector.TxAdditionalLoss,
            TxAntennaHeight = request.Sector.TxAntennaHeight,
            TxAntennaGain = request.Sector.TxAntennaGain,
            TxAzimuth = request.Sector.TxAzimuth,
            TxBeamTilt = request.Sector.TxBeamTilt,
            TxAntennaPatternId = txAntennaPattern.Id,
            TxEIRP = request.Sector.TxEIRP,
        };

        if (request.Sector.IsRxEqualTx)
        {
            newSector.RxAntennaModel = newSector.TxAntennaModel;
            newSector.RxCableType = newSector.TxCableType;
            newSector.RxCableLength = newSector.TxCableLength;
            newSector.RxCableLoss = newSector.TxCableLoss;
            newSector.RxAdditionalLoss = newSector.TxAdditionalLoss;
            newSector.RxAntennaHeight = newSector.TxAntennaHeight;
            newSector.RxAntennaGain = newSector.TxAntennaGain;
            newSector.RxAzimuth = newSector.TxAzimuth;
            newSector.RxBeamTilt = newSector.TxBeamTilt;
            newSector.RxAntennaPatternId = txAntennaPattern.Id;
            newSector.RxEIRP = newSector.TxEIRP;
        }
        else
        {
            newSector.RxAntennaModel = request.Sector.RxAntennaModel;
            newSector.RxCableType = request.Sector.RxCableType;
            newSector.RxCableLength = request.Sector.RxCableLength;
            newSector.RxCableLoss = request.Sector.RxCableLoss;
            newSector.RxAdditionalLoss = request.Sector.RxAdditionalLoss;
            newSector.RxAntennaHeight = request.Sector.RxAntennaHeight;
            newSector.RxAntennaGain = request.Sector.RxAntennaGain;
            newSector.RxAzimuth = request.Sector.RxAzimuth;
            newSector.RxBeamTilt = request.Sector.RxBeamTilt;
            newSector.RxAntennaPatternId = rxAntennaPattern.Id;
            newSector.RxEIRP = request.Sector.RxEIRP;
        }

        _sectorRepository.Add(newSector);
        await _sectorRepository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<CreateSectorDto>
        {
            Data = new CreateSectorDto
            {
                NetworkId = newSector.NetworkId,
                MIMOId = newSector.MIMOId,
                CellId = newSector.CellId,
                AntennaType = EnumHelper<AntennaTypeEnum>.GetDescription(newSector.AntennaType),
                Number = newSector.Number,
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
                TxAntennaPatternId = newSector.TxAntennaPatternId,
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
                RxAntennaPatternId = newSector.RxAntennaPatternId,
                RxEIRP = newSector.RxEIRP
            },
            Succeeded = true,
            Message = "Sector created successfully."
        };
    }
}
