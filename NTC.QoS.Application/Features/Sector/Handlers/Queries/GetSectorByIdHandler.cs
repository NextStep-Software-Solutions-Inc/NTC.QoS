using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Queries;

public class GetSectorByIdHandler(ISectorRepository sectorRepository) : IRequestHandler<GetSectorByIdRequest, ApiResponse<SectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    public async Task<ApiResponse<SectorDto>> Handle(GetSectorByIdRequest request, CancellationToken cancellationToken)
    {
        var sector = await _sectorRepository.GetByIdAsync(request.Id, cancellationToken);
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
            Message = "Sector retrieved successfully.",
            Succeeded = true
        };
    }
}
