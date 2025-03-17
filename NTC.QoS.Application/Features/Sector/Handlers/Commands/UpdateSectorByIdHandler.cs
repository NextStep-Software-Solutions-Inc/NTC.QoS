using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Commands;

public class UpdateSectorByIdHandler(ISectorRepository sectorRepository) : IRequestHandler<UpdateSectorByIdRequest, ApiResponse<UpdateSectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    public async Task<ApiResponse<UpdateSectorDto>> Handle(UpdateSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var antennaType = EnumHelper<AntennaTypeEnum>.ParseUserInput(request.Sector.AntennaType);

        var sector = await _sectorRepository.GetByIdAsync(request.Id, cancellationToken);
        if (sector is null)
        {
            return new ApiResponse<UpdateSectorDto>
            {
                Succeeded = false,
                Error = new[] { $"Sector with ID [{request.Id}] not found." },
                Message = $"Sector does not exist."
            };
        }

        var existingSectors = await _sectorRepository.GetAllSectorsAsync(cancellationToken);
        if (existingSectors.Any(s => s.Id != request.Id &&
                                    s.NetworkId == request.Sector.NetworkId &&
                                    s.Number == request.Sector.Number &&
                                    s.CellId == request.Sector.CellId &&
                                    s.AntennaType == antennaType))
        {
            return new ApiResponse<UpdateSectorDto>
            {
                Succeeded = false,
                Error = new[] { "A sector with the same NetworkId, Number, CellId, and AntennaType already exists." },
                Message = "Duplicate sector data."
            };
        }

        sector.NetworkId = request.Sector.NetworkId;
        sector.AntennaType = antennaType;
        sector.Number = request.Sector.Number;
        sector.MIMOId = request.Sector.MIMOId;
        sector.CellId = request.Sector.CellId;
        sector.RadioEquipment = request.Sector.RadioEquipment;
        sector.IsRxEqualTx = request.Sector.IsRxEqualTx;
        sector.TxPower = request.Sector.TxPower;
        sector.TxAntennaModel = request.Sector.TxAntennaModel;
        sector.TxCableType = request.Sector.TxCableType;
        sector.TxCableLength = request.Sector.TxCableLength;
        sector.TxCableLoss = request.Sector.TxCableLoss;
        sector.TxAdditionalLoss = request.Sector.TxAdditionalLoss;
        sector.TxAntennaHeight = request.Sector.TxAntennaHeight;
        sector.TxAntennaGain = request.Sector.TxAntennaGain;
        sector.TxAzimuth = request.Sector.TxAzimuth;
        sector.TxBeamTilt = request.Sector.TxBeamTilt;
        sector.TxAntennaPatternId = request.Sector.TxAntennaPatternId;
        sector.TxEIRP = request.Sector.TxEIRP;

        if (request.Sector.IsRxEqualTx)
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
            sector.RxAntennaModel = request.Sector.RxAntennaModel;
            sector.RxCableType = request.Sector.RxCableType;
            sector.RxCableLength = request.Sector.RxCableLength;
            sector.RxCableLoss = request.Sector.RxCableLoss;
            sector.RxAdditionalLoss = request.Sector.RxAdditionalLoss;
            sector.RxAntennaHeight = request.Sector.RxAntennaHeight;
            sector.RxAntennaGain = request.Sector.RxAntennaGain;
            sector.RxAzimuth = request.Sector.RxAzimuth;
            sector.RxBeamTilt = request.Sector.RxBeamTilt;
            sector.RxAntennaPatternId = request.Sector.RxAntennaPatternId;
            sector.RxEIRP = request.Sector.RxEIRP;
        }

        _sectorRepository.Update(sector);
        await _sectorRepository.SaveChangesAsync(cancellationToken);

        var result = new UpdateSectorDto
        {
            NetworkId = sector.NetworkId,
            AntennaType = EnumHelper<AntennaTypeEnum>.GetDescription(sector.AntennaType),
            Number = sector.Number,
            MIMOId = sector.MIMOId,
            CellId = sector.CellId,
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
            TxAntennaPatternId = sector.TxAntennaPatternId,
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
            RxAntennaPatternId = sector.RxAntennaPatternId,
            RxEIRP = sector.RxEIRP
        };

        return new ApiResponse<UpdateSectorDto>
        {
            Data = result,
            Message = "Sector updated successfully.",
            Succeeded = true
        };
    }
}
