using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Commands;

/// <summary>
/// Handler for updating an existing Speedtest.
/// Input validation is performed by UpdateSpeedtestRequestValidator via FluentValidation pipeline.
/// Business rule validation (e.g., duplicate checking) is performed within this handler.
/// </summary>
public class UpdateSpeedtestHandler(
    ISpeedtestRepository speedtestRepository,
    IServiceProviderRepository serviceProviderRepository,
    IGenerationRepository generationRepository,
    ITechnologyRepository technologyRepository) : IRequestHandler<UpdateSpeedtestRequest, ApiResponse<SpeedtestDto>>
{
    private readonly ISpeedtestRepository _speedtestRepository = speedtestRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IGenerationRepository _generationRepository = generationRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;

    public async Task<ApiResponse<SpeedtestDto>> Handle(UpdateSpeedtestRequest request, CancellationToken cancellationToken)
    {
        // All validation is handled by UpdateSpeedtestRequestValidator via FluentValidation pipeline
        // Fetch existing Speedtest (already validated to exist)
        var speedtestQuery = _speedtestRepository.Query(s => s.Id == request.Id);
        var existingSpeedtest = await _speedtestRepository.FirstOrDefaultAsync(speedtestQuery, cancellationToken);

        // Null check: entity may have been deleted between validation and handling
        if (existingSpeedtest == null)
        {
            return new ApiResponse<SpeedtestDto>
            {
                Message = "Speedtest not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NotFound", ["The requested Speedtest does not exist."] }
                }
            };
        }

        // Update only the fields that are provided (partial update/PATCH behavior)
        Point? newLocation = null;

        // Update location if both coordinates are provided
        if (request.Speedtest.Latitude.HasValue && request.Speedtest.Longitude.HasValue)
        {
            newLocation = GeometryHelper.CreatePoint(x: request.Speedtest.Longitude.Value, y: request.Speedtest.Latitude.Value);
            existingSpeedtest.Location = newLocation;
        }

        // Update ServiceProviderId if provided
        if (request.Speedtest.ServiceProviderId.HasValue)
        {
            existingSpeedtest.ServiceProviderId = request.Speedtest.ServiceProviderId.Value;
        }

        // Update GenerationId if provided (nullable)
        if (request.Speedtest.GenerationId.HasValue)
        {
            existingSpeedtest.GenerationId = request.Speedtest.GenerationId.Value;
        }

        // Update TechnologyId if provided (nullable)
        if (request.Speedtest.TechnologyId.HasValue)
        {
            existingSpeedtest.TechnologyId = request.Speedtest.TechnologyId.Value;
        }

        // Update DateTime if provided
        if (request.Speedtest.DateTime.HasValue)
        {
            existingSpeedtest.DateTime = request.Speedtest.DateTime.Value;
        }

        // Update Ping if provided (nullable, store 0 if null)
        if (request.Speedtest.Ping.HasValue)
        {
            existingSpeedtest.Ping = request.Speedtest.Ping.Value;
        }

        // Update Jitter if provided (nullable, store 0 if null)
        if (request.Speedtest.Jitter.HasValue)
        {
            existingSpeedtest.Jitter = request.Speedtest.Jitter.Value;
        }

        // Update RSSI if provided
        if (request.Speedtest.RSSI.HasValue)
        {
            existingSpeedtest.RSSI = request.Speedtest.RSSI.Value;
        }

        // Update UploadSpeed if provided
        if (request.Speedtest.UploadSpeed.HasValue)
        {
            existingSpeedtest.UploadSpeed = request.Speedtest.UploadSpeed.Value;
        }

        // Update DownloadSpeed if provided
        if (request.Speedtest.DownloadSpeed.HasValue)
        {
            existingSpeedtest.DownloadSpeed = request.Speedtest.DownloadSpeed.Value;
        }

        // Update Address if provided
        if (request.Speedtest.Address != null)
        {
            existingSpeedtest.Address = request.Speedtest.Address;
        }

        // Update PhoneModel if provided
        if (request.Speedtest.PhoneModel != null)
        {
            existingSpeedtest.PhoneModel = request.Speedtest.PhoneModel;
        }

        // Update MCC if provided
        if (request.Speedtest.MCC != null)
        {
            existingSpeedtest.MCC = request.Speedtest.MCC;
        }

        // Update MNC if provided
        if (request.Speedtest.MNC != null)
        {
            existingSpeedtest.MNC = request.Speedtest.MNC;
        }

        // Update TAC if provided
        if (request.Speedtest.TAC != null)
        {
            existingSpeedtest.TAC = request.Speedtest.TAC;
        }

        // Update SignalQuality if provided
        if (request.Speedtest.SignalQuality != null)
        {
            if (!string.IsNullOrEmpty(request.Speedtest.SignalQuality))
            {
                var signalQualityEnum = EnumHelper<SignalQualityEnum>.ParseUserInput(request.Speedtest.SignalQuality);
                existingSpeedtest.SignalQuality = signalQualityEnum.ToString();
            }
            else
            {
                existingSpeedtest.SignalQuality = null;
            }
        }

        // Update CellIdentifier if provided
        if (request.Speedtest.CellIdentifier != null)
        {
            existingSpeedtest.CellIdentifier = request.Speedtest.CellIdentifier;
        }

        // Update IsFixedBroadband if provided
        if (request.Speedtest.IsFixedBroadband.HasValue)
        {
            existingSpeedtest.IsFixedBroadband = request.Speedtest.IsFixedBroadband.Value;
        }

        // IsFileUploaded remains false for manual CRUD operations
        existingSpeedtest.IsFileUploaded = false;

        _speedtestRepository.Update(existingSpeedtest);
        await _speedtestRepository.SaveChangesAsync(cancellationToken);

        // Fetch related data for the response
        var serviceProviderQuery = _serviceProviderRepository
            .QueryAsNoTracking(sp => sp.Id == existingSpeedtest.ServiceProviderId);
        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);

        Domain.Aggregates.Generation? generation = null;
        if (existingSpeedtest.GenerationId.HasValue)
        {
            var generationQuery = _generationRepository
                .QueryAsNoTracking(g => g.Id == existingSpeedtest.GenerationId.Value);
            generation = await _generationRepository.FirstOrDefaultAsync(generationQuery, cancellationToken);
        }

        Domain.Entities.Technology? technology = null;
        if (existingSpeedtest.TechnologyId.HasValue)
        {
            var technologyQuery = _technologyRepository
                .QueryAsNoTracking(t => t.Id == existingSpeedtest.TechnologyId.Value);
            technology = await _technologyRepository.FirstOrDefaultAsync(technologyQuery, cancellationToken);
        }

        // Return updated Speedtest with related data
        return new ApiResponse<SpeedtestDto>
        {
            Data = new SpeedtestDto
            {
                Id = existingSpeedtest.Id,
                ServiceProvider = serviceProvider != null ? new ServiceProviderDto
                {
                    Id = serviceProvider.Id,
                    Service = serviceProvider.Service != null ? new Domain.Models.DTOs.Service.ServiceDto
                    {
                        Id = serviceProvider.Service.Id,
                        Name = serviceProvider.Service.Name,
                        Type = serviceProvider.Service.Type.ToString()
                    } : null,
                    Provider = serviceProvider.Provider != null ? new Domain.Models.DTOs.Provider.ProviderDto
                    {
                        Id = serviceProvider.Provider.Id,
                        Name = serviceProvider.Provider.Name,
                        Description = serviceProvider.Provider.Description,
                        LicenseNumber = serviceProvider.Provider.LicenseNumber,
                        Color = serviceProvider.Provider.Color
                    } : null
                } : null,
                Generation = generation != null ? new GenerationDto
                {
                    Id = generation.Id,
                    Name = generation.Name,
                    Description = generation.Description
                } : null,
                Technology = technology != null ? new TechnologyDto
                {
                    Id = technology.Id,
                    Standard = technology.Standard,
                    Description = technology.Description,
                    Generation = generation != null ? new GenerationDto
                    {
                        Id = generation.Id,
                        Name = generation.Name,
                        Description = generation.Description
                    } : null
                } : null,
                DateTime = existingSpeedtest.DateTime,
                Ping = existingSpeedtest.Ping,
                Jitter = existingSpeedtest.Jitter,
                RSSI = existingSpeedtest.RSSI,
                UploadSpeed = existingSpeedtest.UploadSpeed,
                DownloadSpeed = existingSpeedtest.DownloadSpeed,
                Latitude = existingSpeedtest.Location.Coordinate.Y,
                Longitude = existingSpeedtest.Location.Coordinate.X,
                PhoneModel = existingSpeedtest.PhoneModel,
                MCC = existingSpeedtest.MCC,
                MNC = existingSpeedtest.MNC,
                TAC = existingSpeedtest.TAC,
                SignalQuality = existingSpeedtest.SignalQuality,
                CellIdentifier = existingSpeedtest.CellIdentifier,
                Address = existingSpeedtest.Address,
                IsFileUploaded = existingSpeedtest.IsFileUploaded,
                IsFixedBroadband = existingSpeedtest.IsFixedBroadband,
                Distance = 0
            },
            Message = "Speedtest updated successfully.",
            Succeeded = true
        };
    }
}
