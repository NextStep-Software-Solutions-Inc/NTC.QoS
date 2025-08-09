using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Commands;

public class CreateSpeedtestHandler(
    ISpeedtestRepository speedtestRepository,
    IServiceProviderRepository serviceProviderRepository,
    IGenerationRepository generationRepository,
    ITechnologyRepository technologyRepository) : IRequestHandler<CreateSpeedtestRequest, ApiResponse<SpeedtestDto>>
{
    private readonly ISpeedtestRepository _speedtestRepository = speedtestRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IGenerationRepository _generationRepository = generationRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;

    public async Task<ApiResponse<SpeedtestDto>> Handle(CreateSpeedtestRequest request, CancellationToken cancellationToken)
    {
        Point location = GeometryHelper.CreatePoint(x: request.Speedtest.Longitude, y: request.Speedtest.Latitude);

        var serviceProviderQuery = _serviceProviderRepository.Query(sp => sp.Id == request.Speedtest.ServiceProviderId);
        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);

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

        Domain.Aggregates.Generation? generation = null;
        if (request.Speedtest.GenerationId.HasValue)
        {
            var generationQuery = _generationRepository.Query(g => g.Id == request.Speedtest.GenerationId.Value);
            generation = await _generationRepository.FirstOrDefaultAsync(generationQuery, cancellationToken);

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
        }

        Domain.Entities.Technology? technology = null;
        if (request.Speedtest.TechnologyId.HasValue)
        {
            var technologyQuery = _technologyRepository.Query(t => t.Id == request.Speedtest.TechnologyId.Value);
            technology = await _technologyRepository.FirstOrDefaultAsync(technologyQuery, cancellationToken);

            if (technology is null)
            {
                return new ApiResponse<SpeedtestDto>
                {
                    Message = "Technology does not exist.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        {
                            "NullTechnology", ["Technology not found."]
                        }
                    }
                };
            }
        }

        string? validatedSignalQuality = null;
        if (!string.IsNullOrEmpty(request.Speedtest.SignalQuality))
        {
            var signalQualityEnum = EnumHelper<SignalQualityEnum>.ParseUserInput(request.Speedtest.SignalQuality);
            validatedSignalQuality = signalQualityEnum.ToString();
        }

        var existingSpeedtest = await _speedtestRepository.FirstOrDefaultAsync(
            _speedtestRepository.Query(s => s.Address == request.Speedtest.Address &&
                                            s.ServiceProviderId == request.Speedtest.ServiceProviderId &&
                                            s.GenerationId == request.Speedtest.GenerationId &&
                                            s.TechnologyId == request.Speedtest.TechnologyId &&
                                            s.DateTime == request.Speedtest.DateTime),
            cancellationToken);

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
            Address = request.Speedtest.Address,
            ServiceProviderId = serviceProvider.Id,
            GenerationId = request.Speedtest.GenerationId,
            TechnologyId = request.Speedtest.TechnologyId,
            DateTime = request.Speedtest.DateTime,
            Ping = request.Speedtest.Ping,
            Jitter = request.Speedtest.Jitter,
            RSSI = request.Speedtest.RSSI,
            UploadSpeed = request.Speedtest.UploadSpeed,
            DownloadSpeed = request.Speedtest.DownloadSpeed,
            PhoneModel = request.Speedtest.PhoneModel,
            MCC = request.Speedtest.MCC,
            MNC = request.Speedtest.MNC,
            TAC = request.Speedtest.TAC,
            SignalQuality = validatedSignalQuality,
            CellIdentifier = request.Speedtest.CellIdentifier,
            IsFixedBroadband = request.Speedtest.IsFixedBroadband,
            Location = location
        };

        _speedtestRepository.Add(newSpeedtest);
        await _speedtestRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<SpeedtestDto>
        {
            Data = new SpeedtestDto
            {
                Id = newSpeedtest.Id,
                ServiceProvider = new()
                {
                    Id = serviceProvider.Id,
                    Service = new()
                    {
                        Id = serviceProvider.ServiceId,
                        Name = serviceProvider.Service?.Name ?? string.Empty,
                        Type = serviceProvider.Service?.Type.ToString() ?? string.Empty
                    },
                    Provider = new()
                    {
                        Id = serviceProvider.ProviderId,
                        Name = serviceProvider.Provider?.Name ?? string.Empty,
                        Description = serviceProvider.Provider?.Description,
                        LicenseNumber = serviceProvider.Provider?.LicenseNumber,
                        Color = serviceProvider.Provider?.Color ?? string.Empty
                    }
                },
                Generation = generation is not null ? new()
                {
                    Id = generation.Id,
                    Name = generation.Name,
                    Description = generation.Description,
                } : null,
                DateTime = newSpeedtest.DateTime,
                Ping = newSpeedtest.Ping,
                Jitter = newSpeedtest.Jitter,
                RSSI = newSpeedtest.RSSI,
                UploadSpeed = newSpeedtest.UploadSpeed,
                DownloadSpeed = newSpeedtest.DownloadSpeed,
                Latitude = newSpeedtest.Location.Coordinate.Y,
                Longitude = newSpeedtest.Location.Coordinate.X,
                PhoneModel = newSpeedtest.PhoneModel,
                MCC = newSpeedtest.MCC,
                MNC = newSpeedtest.MNC,
                TAC = newSpeedtest.TAC,
                SignalQuality = newSpeedtest.SignalQuality,
                CellIdentifier = newSpeedtest.CellIdentifier,
                Distance = 0,
                Address = newSpeedtest.Address,
                IsFixedBroadband = newSpeedtest.IsFixedBroadband,
            },
            Message = "Speedtest created successfully.",
            Succeeded = true
        };
    }
}
