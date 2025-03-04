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
            BarangayId = s.BarangayId,
            Barangay = new BarangayDto
            {
                Id = s.BarangayId,
                Name = s.Barangay?.Name ?? "Unknown"
            },
            ServiceProviderId = s.ServiceProviderId,
            ServiceProvider = new ServiceProviderDto
            {
                Id = s.ServiceProviderId,
            },
            GenerationId = s.GenerationId,
            Generation = new GenerationDto
            {
                Id = s.GenerationId,
                Name = s.Generation?.Name ?? "Unknown"
            },
            CellId = s.CellId,
            Cell = new CellDto
            {
                Id = s.CellId,
                Name = s.Cell?.Name ?? "Unknown"
            },
            FileId = s.FileId,
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
