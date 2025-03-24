using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Generation;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.File;

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

        if (speedtest is null)
        {
            return new ApiResponse<SpeedtestDto>
            {
                Succeeded = false,
                Message = "Speedtest could not be found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", new[] { $"Speedtest with ID [{request.Id}] does not exist." } } }
            };
        }

        return new ApiResponse<SpeedtestDto>
        {
            Succeeded = true,
            Message = "Speedtest retrieved successfully.",
            Data = new SpeedtestDto()
            {
                Id = speedtest.Id,
                Barangay = new BarangaySpeedtestDto
                {
                    Id = speedtest.BarangayId,
                    Name = speedtest.Barangay?.Name ?? "Unknown"
                },
                ServiceProvider = new ServiceProviderSpeedtestDto
                {
                    Id = speedtest.ServiceProviderId,
                    Provider = new ProviderSpeedtestDto
                    {
                        Id = speedtest.ServiceProvider?.ProviderId ?? Guid.Empty,
                        Name = speedtest.ServiceProvider?.Provider?.Name ?? "Unknown",
                    },
                    Service = new ServiceSpeedtestDto
                    {
                        Id = speedtest.ServiceProvider?.ServiceId ?? Guid.Empty,
                        Name = speedtest.ServiceProvider?.Service?.Name ?? "Unknown",
                    }
                },
                Generation = new GenerationSpeedtestDto
                {
                    Id = speedtest.GenerationId,
                    Name = speedtest.Generation?.Name ?? "Unknown",
                    Description = speedtest.Generation?.Description ?? "Unknown"
                },
                Cell = new CellSpeedtestDto
                {
                    Id = speedtest.CellId,
                    Name = speedtest.Cell?.Name ?? "Unknown",
                    Group = speedtest.Cell?.Group ?? "Unknown"
                },
                File = new FileDto
                {
                    Id = speedtest.FileId,
                    Name = speedtest.File?.Name ?? "Unknown",
                    Path = speedtest.File?.Path ?? "Unknown",
                },
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
