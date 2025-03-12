using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Generation;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Queries;

public class GetAllSpeedtestsHandler : IRequestHandler<GetAllSpeedtestsRequest, PaginatedResponse<SpeedtestDto>>
{
    private readonly ISpeedtestRepository _speedtestRepository;

    public GetAllSpeedtestsHandler(ISpeedtestRepository speedtestRepository)
    {
        _speedtestRepository = speedtestRepository;
    }

    public async Task<PaginatedResponse<SpeedtestDto>> Handle(GetAllSpeedtestsRequest request, CancellationToken cancellationToken)
    {
        var allSpeedtests = await _speedtestRepository.ListAsync(s => !s.IsDeleted, cancellationToken);

        if (request.BarangayId.HasValue)
        {
            allSpeedtests = allSpeedtests.Where(s => s.BarangayId == request.BarangayId.Value).ToList();
        }

        if (request.ServiceProviderId.HasValue)
        {
            allSpeedtests = allSpeedtests.Where(s => s.ServiceProviderId == request.ServiceProviderId.Value).ToList();
        }

        if (request.GenerationId.HasValue)
        {
            allSpeedtests = allSpeedtests.Where(s => s.GenerationId == request.GenerationId.Value).ToList();
        }

        if (request.CellId.HasValue)
        {
            allSpeedtests = allSpeedtests.Where(s => s.CellId == request.CellId.Value).ToList();
        }

        if (request.SignalQuality.HasValue)
        {
            allSpeedtests = allSpeedtests.Where(s => s.SignalQuality == request.SignalQuality.Value).ToList();
        }

        var speedtestDtos = allSpeedtests.Select(s => new SpeedtestDto
        {
            Id = s.Id,
            Barangay = new BarangaySpeedtestDto
            {
                Id = s.BarangayId,
                Name = s.Barangay?.Name ?? "Unknown"
            },
            ServiceProvider = new ServiceProviderSpeedtestDto
            {
                Id = s.ServiceProviderId,
                Provider = new ProviderSpeedtestDto
                {
                    Id = s.ServiceProvider?.ProviderId ?? Guid.Empty,
                    Name = s.ServiceProvider?.Provider?.Name ?? "Unknown",
                },
                Service = new ServiceSpeedtestDto
                {
                    Id = s.ServiceProvider?.ServiceId ?? Guid.Empty,
                    Name = s.ServiceProvider?.Service?.Name ?? "Unknown",
                }
            },
            Generation = new GenerationSpeedtestDto
            {
                Id = s.GenerationId,
                Name = s.Generation?.Name ?? "Unknown",
                Description = s.Generation?.Description ?? "Unknown"
            },
            Cell = new CellSpeedtestDto
            {
                Id = s.CellId,
                Name = s.Cell?.Name ?? "Unknown",
                Group = s.Cell?.Group ?? "Unknown"
            },
            File = new FileDto
            {
                Id = s.FileId,
                Name= s.File?.Name ?? "Unknown",
                Path = s.File?.Path ?? "Unknown",   
            },
            DateTime = s.DateTime,
            Ping = s.Ping,
            Jitter = s.Jitter,
            RSSI = s.RSSI,
            UploadSpeed = s.UploadSpeed,
            DownloadSpeed = s.DownloadSpeed,
            CoorLatitude = s.CoorLatitude,
            CoorLongitude = s.CoorLongitude,
            PhoneModel = s.PhoneModel,
            MCC = s.MCC,
            MNC = s.MNC,
            TAC = s.TAC,
            SignalQuality = s.SignalQuality
        }).ToList();

        return new PaginatedResponse<SpeedtestDto>(speedtestDtos, request.PageNumber, request.PageSize);
    }
}
