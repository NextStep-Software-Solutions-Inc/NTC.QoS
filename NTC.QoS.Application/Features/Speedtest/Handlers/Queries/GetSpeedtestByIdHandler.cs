using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Queries;

public class GetSpeedtestByIdHandler(ISpeedtestRepository speedtestRepository) : IRequestHandler<GetSpeedtestByIdRequest, ApiResponse<SpeedtestDto>>
{
    private readonly ISpeedtestRepository _speedtestRepository = speedtestRepository;

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
                ServiceProvider = new()
                {
                    Id = speedtest!.ServiceProviderId,
                    Service = new()
                    {
                        Id = speedtest.ServiceProvider?.Service?.Id ?? Guid.Empty,
                        Name = speedtest.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = speedtest.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                    },
                    Provider = new()
                    {
                        Id = speedtest.ServiceProvider?.Provider?.Id ?? Guid.Empty,
                        Name = speedtest.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Description = speedtest.ServiceProvider?.Provider?.Description,
                        LicenseNumber = speedtest.ServiceProvider?.Provider?.LicenseNumber,
                        Color = speedtest.ServiceProvider?.Provider?.Color ?? string.Empty
                    }
                },
                Generation = new()
                {
                    Id = speedtest.GenerationId,
                    Name = speedtest.Generation?.Name ?? string.Empty,
                    Description = speedtest.Generation?.Description,
                },
                DateTime = speedtest.DateTime,
                Ping = speedtest.Ping,
                Jitter = speedtest.Jitter,
                RSSI = speedtest.RSSI,
                UploadSpeed = speedtest.UploadSpeed,
                DownloadSpeed = speedtest.DownloadSpeed,
                Latitude = speedtest.Location.Coordinate.Y,
                Longitude = speedtest.Location.Coordinate.X,
                PhoneModel = speedtest.PhoneModel,
                MCC = speedtest.MCC,
                MNC = speedtest.MNC,
                TAC = speedtest.TAC,
                SignalQuality = speedtest.SignalQuality,
                CellIdentifier = speedtest.CellIdentifier,
                Address = speedtest.Address ?? string.Empty,
                Distance = Math.Round(speedtest.Location.Distance(speedtest.Location), 2)
            }
        };
    }
}
