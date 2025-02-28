using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Generation;
using NTC.QoS.Domain.Models.DTOs.Cell;

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
            allSpeedtests = allSpeedtests.Where(m => m.GenerationId == request.GenerationId.Value).ToList();
        }

        if (request.CellId.HasValue)
        {
            allSpeedtests = allSpeedtests.Where(s => s.CellId == request.CellId.Value).ToList();
        }

        if (request.SignalQuality.HasValue)
        {
            allSpeedtests = allSpeedtests.Where(s => s.SignalQuality == request.SignalQuality.Value).ToList();
        }

        var speedtestDtos = allSpeedtests.Select(p => new SpeedtestDto
        {
            Id = p.Id,
            BarangayId = p.BarangayId,
            Barangay = new BarangayDto
            {
                Id = p.BarangayId,
                Name = p.Barangay?.Name ?? "Unknown"
            },
            ServiceProviderId = p.ServiceProviderId,
            ServiceProvider = new ServiceProviderDto
            {
                Id = p.ServiceProviderId,
            },
            GenerationId = p.GenerationId,
            Generation = new GenerationDto
            {
                Id = p.GenerationId,
                Name = p.Generation?.Name ?? "Unknown"
            },
            CellId = p.CellId,
            Cell = new CellDto
            {
                Id = p.CellId,
                Name = p.Cell?.Name ?? "Unknown"
            },
            FileId = p.FileId,
            DateTime = p.DateTime,
            Ping = p.Ping,
            Jitter = p.Jitter,
            RSSI = p.RSSI,
            UploadSpeed = p.UploadSpeed,
            DownloadSpeed = p.DownloadSpeed,
            CoorLatitude = p.CoorLatitude,
            CoorLongitude = p.CoorLongitude,
            PhoneModel = p.PhoneModel,
            MCC = p.MCC,
            MNC = p.MNC,
            TAC = p.TAC,
            SignalQuality = p.SignalQuality
        }).ToList();

        return new PaginatedResponse<SpeedtestDto>(speedtestDtos, request.PageNumber, request.PageSize);
    }
}
