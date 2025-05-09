using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

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
                Barangay = new()
                {
                    Id = speedtest.BarangayId,
                    Name = speedtest.Barangay?.Name ?? string.Empty,
                    Description = speedtest.Barangay?.Description,
                    Municipality = new()
                    {
                        Id = speedtest.Barangay?.MunicipalityId ?? Guid.Empty,
                        Name = speedtest.Barangay?.Municipality?.Name ?? string.Empty,
                        Description = speedtest.Barangay?.Municipality?.Description,
                        IsCity = speedtest.Barangay?.Municipality?.IsCity ?? false,
                        Province = new()
                        {
                            Id = speedtest.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                            Name = speedtest.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                            Description = speedtest.Barangay?.Municipality?.Province?.Description,
                            Region = new()
                            {
                                Id = speedtest.Barangay?.Municipality?.Province?.RegionId ?? Guid.Empty,
                                Name = speedtest.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                Description = speedtest.Barangay?.Municipality?.Province?.Region?.Description
                            }
                        }
                    }
                },
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
                CoorLatitude = speedtest.CoorLatitude,
                CoorLongitude = speedtest.CoorLongitude ?? string.Empty,
                PhoneModel = speedtest.PhoneModel,
                MCC = speedtest.MCC,
                MNC = speedtest.MNC,
                TAC = speedtest.TAC,
                SignalQuality = speedtest.SignalQuality,
                CellIdentifier = speedtest.CellIdentifier,
            }
        };
    }
}
