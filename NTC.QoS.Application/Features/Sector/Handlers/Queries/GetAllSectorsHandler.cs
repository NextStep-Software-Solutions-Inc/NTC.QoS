using MediatR;
using NTC.QoS.Application.Features.Sector.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Handlers.Queries;

public class GetAllSectorsHandler(ISectorRepository sectorRepository) : IRequestHandler<GetAllSectorsRequest, PaginatedResponse<SectorDto>>
{
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    public async Task<PaginatedResponse<SectorDto>> Handle(GetAllSectorsRequest request, CancellationToken cancellationToken)
    {
        var allSectors = await _sectorRepository.ListAsync(s => !s.IsDeleted, cancellationToken);

        allSectors = [.. allSectors
            .Where(s => !request.NetworkId.HasValue || s.NetworkId == request.NetworkId)
            .Where(s => !request.MIMOId.HasValue || s.MIMOId == request.MIMOId)
            .Where(s => !request.CellId.HasValue || s.CellId == request.CellId)
            .Where(s => !request.TxAntennaPatternId.HasValue || s.TxAntennaPatternId == request.TxAntennaPatternId)
            .Where(s => !request.RxAntennaPatternId.HasValue || s.RxAntennaPatternId == request.RxAntennaPatternId)];

        var allSectorDtos = allSectors.Select(s => new SectorDto
        {
            Id = s.Id,
            Network = new NetworkSectorDto 
            { 
                Id = s.NetworkId,
                Name = s.Network?.Name ?? string.Empty,
                SystemType = s.Network?.SystemType.ToString() ?? string.Empty,
            },
            AntennaType = EnumHelper<AntennaTypeEnum>.GetDescription(s.AntennaType),
            Number = s.Number,
            MIMO = new MIMOSectorDto 
            { 
                Id = s.MIMOId ?? Guid.Empty,
                Type = s.MIMO?.Type.ToString() ?? string.Empty 
            },
            Cell = new CellSectorDto
            {
                Id = s.Cell?.Id ?? Guid.Empty,
                SiteId = s.Cell?.SiteId ?? string.Empty,
                Name = s.Cell?.Name ?? string.Empty,
                Description = s.Cell?.Description ?? string.Empty,
                TowerType = s.Cell?.TowerType.ToString() ?? string.Empty,
                ServiceProvider = new ServiceProviderSectorDto
                {
                    Id = s.Cell?.ServiceProvider?.Id ?? Guid.Empty,
                    Provider = new ProviderSectorDto 
                    { 
                        Id = s.Cell?.ServiceProvider?.Provider?.Id ?? Guid.Empty,
                        Name = s.Cell?.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Description = s.Cell?.ServiceProvider?.Provider?.Description ?? string.Empty,
                    },
                    Service = new ServiceSectorDto
                    {
                        Id = s.Cell?.ServiceProvider?.Service?.Id ?? Guid.Empty,
                        Name = s.Cell?.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = s.Cell?.ServiceProvider?.Service?.Type.ToString() ?? string.Empty,
                    },
                },
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
            TxAntennaPattern = new AntennaPatternSectorDto
            {
                Id = s.TxAntennaPatternId,
                Name = s.TxAntennaPattern?.Name ?? string.Empty,
                URL = s.TxAntennaPattern?.URL ?? string.Empty,
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
            RxAntennaPattern = new AntennaPatternSectorDto
            {
                Id = s.RxAntennaPatternId,
                Name = s.RxAntennaPattern?.Name ?? string.Empty,
                URL = s.RxAntennaPattern?.URL ?? string.Empty,
            },
            RxEIRP = s.RxEIRP
        }).ToList();

        if (allSectorDtos.Count is 0)
        {
            return new PaginatedResponse<SectorDto>(allSectorDtos, request.PageNumber, request.PageSize)
            {
                Data = allSectorDtos,
                Message = "No records found.",
                Succeeded = false
            };
        }

        return new PaginatedResponse<SectorDto>(allSectorDtos, request.PageNumber, request.PageSize)
        {
            Data = allSectorDtos,
            Message = "Sectors retrieved successfully.",
            Succeeded = true
        };
    }
}
