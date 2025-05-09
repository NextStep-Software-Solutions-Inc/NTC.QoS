using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Commands;

public class CreateSpeedtestHandler(ISpeedtestRepository speedtestRepository,
                                    IFileRepository fileRepository,
                                    IBarangayRepository barangayRepository,
                                    IServiceProviderRepository serviceProviderRepository,
                                    IGenerationRepository generationRepository) : IRequestHandler<CreateSpeedtestRequest, ApiResponse<SpeedtestDto>>
{
    private readonly ISpeedtestRepository _speedtestRepository = speedtestRepository;
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IGenerationRepository _generationRepository = generationRepository;

    public async Task<ApiResponse<SpeedtestDto>> Handle(CreateSpeedtestRequest request, CancellationToken cancellationToken)
    {
        var barangay = await _barangayRepository.GetByIdAsync(request.Speedtest.BarangayId, cancellationToken);

        if (barangay is null)
        {
            return new ApiResponse<SpeedtestDto>
            {
                Message = "Barangay does not exist.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullBarangay", ["Barangay not found."]
                    }
                }
            };
        }

        var serviceProvider = await _serviceProviderRepository.GetByIdAsync(request.Speedtest.ServiceProviderId, cancellationToken);

        if (serviceProvider is null)
        {
            return new ApiResponse<SpeedtestDto>
            {
                Message = "Service provider does not exist.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullServiceProvider", ["Service provider not found."]
                    }
                }
            };
        }

        var generation = await _generationRepository.GetByIdAsync(request.Speedtest.GenerationId, cancellationToken);

        if (generation is null)
        {
            return new ApiResponse<SpeedtestDto>
            {
                Message = "Generation does not exist.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullGeneration", ["Generation not found."]
                    }
                }
            };
        }

        var existingSpeedtest = await _speedtestRepository.FindAsync(
            s => s.BarangayId == request.Speedtest.BarangayId &&
                 s.ServiceProviderId == request.Speedtest.ServiceProviderId &&
                 s.GenerationId == request.Speedtest.GenerationId &&
                 s.FileId == request.Speedtest.FileId &&
                 s.DateTime == request.Speedtest.DateTime &&
                 !s.IsDeleted, cancellationToken);

        if (existingSpeedtest is not null)
        {
            return new ApiResponse<SpeedtestDto>
            {
                Message = "A Speedtest entry with the same DateTime already exists for this combination.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Date already exists in this combination."]
                    }
                }
            };
        }

        var newSpeedtest = new Domain.Entities.Speedtest
        {
            BarangayId = barangay.Id,
            ServiceProviderId = serviceProvider.Id,
            GenerationId = generation.Id,
            FileId = request.Speedtest.FileId,
            DateTime = request.Speedtest.DateTime,
            Ping = request.Speedtest.Ping,
            Jitter = request.Speedtest.Jitter,
            RSSI = request.Speedtest.RSSI,
            UploadSpeed = request.Speedtest.UploadSpeed,
            DownloadSpeed = request.Speedtest.DownloadSpeed,
            CoorLatitude = request.Speedtest.CoorLatitude,
            CoorLongitude = request.Speedtest.CoorLongitude,
            PhoneModel = request.Speedtest.PhoneModel,
            MCC = request.Speedtest.MCC,
            MNC = request.Speedtest.MNC,
            TAC = request.Speedtest.TAC,
            SignalQuality = request.Speedtest.SignalQuality,
            CellIdentifier = request.Speedtest.CellIdentifier
        };

        _speedtestRepository.Add(newSpeedtest);
        await _speedtestRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<SpeedtestDto>
        {
            Data = new SpeedtestDto
            {
                Id = newSpeedtest.Id,
                Barangay = new ()
                {
                    Id = newSpeedtest.BarangayId,
                    Name = newSpeedtest.Barangay?.Name ?? string.Empty,
                    Description = newSpeedtest.Barangay?.Description,
                    Municipality = new ()
                    {
                        Id = newSpeedtest.Barangay?.MunicipalityId ?? Guid.Empty,
                        Name = newSpeedtest.Barangay?.Municipality?.Name ?? string.Empty,
                        Description = newSpeedtest.Barangay?.Municipality?.Description,
                        IsCity = newSpeedtest.Barangay?.Municipality?.IsCity ?? false,
                        Province = new ()
                        {
                            Id = newSpeedtest.Barangay?.Municipality?.ProvinceId ?? Guid.Empty,
                            Name = newSpeedtest.Barangay?.Municipality?.Province?.Name ?? string.Empty,
                            Description = newSpeedtest.Barangay?.Municipality?.Province?.Description,
                            Region = new ()
                            {
                                Id = newSpeedtest.Barangay?.Municipality?.Province?.RegionId ?? Guid.Empty,
                                Name = newSpeedtest.Barangay?.Municipality?.Province?.Region?.Name ?? string.Empty,
                                Description = newSpeedtest.Barangay?.Municipality?.Province?.Region?.Description
                            }
                        }
                    }
                },
                ServiceProvider = new ()
                {
                    Id = newSpeedtest!.ServiceProviderId,
                    Service = new()
                    {
                        Id = newSpeedtest.ServiceProvider?.Service?.Id ?? Guid.Empty,
                        Name = newSpeedtest.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = newSpeedtest.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                    },
                    Provider = new()
                    {
                        Id = newSpeedtest.ServiceProvider?.Provider?.Id ?? Guid.Empty,
                        Name = newSpeedtest.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Description = newSpeedtest.ServiceProvider?.Provider?.Description,
                        LicenseNumber = newSpeedtest.ServiceProvider?.Provider?.LicenseNumber,
                        Color = newSpeedtest.ServiceProvider?.Provider?.Color ?? string.Empty
                    }
                },
                Generation = new()
                {
                    Id = newSpeedtest.GenerationId,
                    Name = newSpeedtest.Generation?.Name ?? string.Empty,
                    Description = newSpeedtest.Generation?.Description,
                },
                DateTime = newSpeedtest.DateTime,
                Ping = newSpeedtest.Ping,
                Jitter = newSpeedtest.Jitter,
                RSSI = newSpeedtest.RSSI,
                UploadSpeed = newSpeedtest.UploadSpeed,
                DownloadSpeed = newSpeedtest.DownloadSpeed,
                CoorLatitude = newSpeedtest.CoorLatitude,
                CoorLongitude = newSpeedtest.CoorLongitude ?? string.Empty,
                PhoneModel = newSpeedtest.PhoneModel,
                MCC = newSpeedtest.MCC,
                MNC = newSpeedtest.MNC,
                TAC = newSpeedtest.TAC,
                SignalQuality = newSpeedtest.SignalQuality,
                CellIdentifier = newSpeedtest.CellIdentifier,

            },
            Message = "Speedtest created successfully.",
            Succeeded = true
        };
    }
}
