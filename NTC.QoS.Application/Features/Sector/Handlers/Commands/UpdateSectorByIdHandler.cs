using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Commands;

public class UpdateSectorByIdHandler(ISectorRepository sectorRepository) : IRequestHandler<UpdateSectorByIdRequest, ApiResponse<SectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    public async Task<ApiResponse<SectorDto>> Handle(UpdateSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var antennaType = EnumHelper<AntennaTypeEnum>.ParseUserInput(request.Sector.AntennaType);

        var sector = await _sectorRepository.GetByIdAsync(request.Id, cancellationToken);
        if (sector is null)
        {
            return new ApiResponse<SectorDto>
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
            return new ApiResponse<SectorDto>
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
            Cell = new CellSectorDto
            {
                Id = sector.Cell?.Id ?? Guid.Empty,
                SiteId = sector.Cell?.SiteId ?? string.Empty,
                Name = sector.Cell?.Name ?? string.Empty,
                Description = sector.Cell?.Description ?? string.Empty,
                TowerType = sector.Cell?.TowerType.ToString() ?? string.Empty,
                ServiceProvider = new ServiceProviderSectorDto
                {
                    Id = sector.Cell?.ServiceProvider?.Id ?? Guid.Empty,
                    Provider = new ProviderSectorDto
                    {
                        Id = sector.Cell?.ServiceProvider?.Provider?.Id ?? Guid.Empty,
                        Name = sector.Cell?.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Description = sector.Cell?.ServiceProvider?.Provider?.Description ?? string.Empty,
                    },
                    Service = new ServiceSectorDto
                    {
                        Id = sector.Cell?.ServiceProvider?.Service?.Id ?? Guid.Empty,
                        Name = sector.Cell?.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = sector.Cell?.ServiceProvider?.Service?.Type.ToString() ?? string.Empty,
                    },
                },
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
