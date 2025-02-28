using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Generation;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Queries;

public class GetSpeedtestByIdHandler : IRequestHandler<GetSpeedtestByIdRequest, ApiResponse<SpeedtestDto>>
{
    private readonly ISpeedtestRepository _speedtestRepository;

    public GetSpeedtestByIdHandler(ISpeedtestRepository speedtestRepository)
    {
        _speedtestRepository = speedtestRepository;
    }

    public async Task<ApiResponse<SpeedtestDto>> Handle(GetSpeedtestByIdRequest request, CancellationToken cancellationToken)
    {
        var speedtest = await _speedtestRepository.GetByIdAsync(request.Id, cancellationToken);

        if (speedtest == null)
        {
            return new ApiResponse<SpeedtestDto>
            {
                Succeeded = false,
                Message = "Speedtest not found.",
                Error = new[] { "The requested Speedtest does not exist." }
            };
        }

        return new ApiResponse<SpeedtestDto>
        {
            Succeeded = true,
            Message = "Speedtest retrieved successfully",
            Data = new SpeedtestDto()
            {
                Id = speedtest.Id,
                BarangayId = speedtest.BarangayId,
                Barangay = new BarangayDto
                {
                    Id = speedtest.BarangayId,
                    Name = speedtest.Barangay?.Name ?? "Unknown"
                },
                ServiceProviderId = speedtest.ServiceProviderId,
                ServiceProvider = new ServiceProviderDto
                {
                    Id = speedtest.ServiceProviderId,
                },
                GenerationId = speedtest.GenerationId,
                Generation = new GenerationDto
                {
                    Id = speedtest.GenerationId,
                    Name = speedtest.Generation?.Name ?? "Unknown"
                },
                CellId = speedtest.CellId,
                Cell = new CellDto
                {
                    Id = speedtest.CellId,
                    Name = speedtest.Cell?.Name ?? "Unknown"
                },
                FileId = speedtest.FileId,
                DateTime = speedtest.DateTime,
                Ping = speedtest.Ping,
                Jitter = speedtest.Jitter,
                RSSI = speedtest.RSSI,
                UploadSpeed = speedtest.UploadSpeed,
                DownloadSpeed = speedtest.DownloadSpeed,
                CoorLatitude = speedtest.CoorLatitude,
                CoorLongitude = speedtest.CoorLongitude,
                PhoneModel = speedtest.PhoneModel,
                MCC = speedtest.MCC,
                MNC = speedtest.MNC,
                TAC = speedtest.TAC,
                SignalQuality = speedtest.SignalQuality
            }
        };
    }
}
