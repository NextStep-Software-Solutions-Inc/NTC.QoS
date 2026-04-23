using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Queries;

public class GetAllSpeedtestsHandler(ISpeedtestRepository speedtestRepository) : IRequestHandler<GetAllSpeedtestsRequest, PaginatedResponse<SpeedtestDto>>
{
    private readonly ISpeedtestRepository _speedtestRepository = speedtestRepository;

    public async Task<PaginatedResponse<SpeedtestDto>> Handle(GetAllSpeedtestsRequest request, CancellationToken cancellationToken)
    {
        // Start with optimized no-tracking query and apply spatial filtering via repository
        var speedtestQuery = _speedtestRepository.GetEntitiesWithinBoundaries(
            _speedtestRepository.QueryAsNoTracking(),
            regionIds: request.RegionIds,
            provinceIds: request.ProvinceIds,
            municipalityIds: request.MunicipalityIds,
            barangayIds: request.BarangayIds);

        // Apply search filter
        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchLower = request.Search.ToLower();
            speedtestQuery = speedtestQuery.Where(s =>
                (s.PhoneModel != null && s.PhoneModel.ToLower().Contains(searchLower)) ||
                (s.MCC != null && s.MCC.ToLower().Contains(searchLower)) ||
                (s.MNC != null && s.MNC.ToLower().Contains(searchLower)) ||
                (s.TAC != null && s.TAC.ToLower().Contains(searchLower)));
        }

        // Apply filters - use local variables to avoid closure issues
        var serviceProviderIds = request.ServiceProviderIds;
        var providerIds = request.ProviderIds;
        var serviceIds = request.ServiceIds;
        var generationIds = request.GenerationIds;
        var technologyIds = request.TechnologyIds;
        var signalQuality = request.SignalQuality;

        // Apply speed range filters if specified
        var minDownloadSpeed = request.MinDownloadSpeed;
        var maxDownloadSpeed = request.MaxDownloadSpeed;
        var minUploadSpeed = request.MinUploadSpeed;
        var maxUploadSpeed = request.MaxUploadSpeed;
        var startDate = request.StartDate;
        var endDate = request.EndDate;
        var isNoBroadbandCoverage = request.IsNoBroadbandCoverage;

        // Combine all filters in a single Where clause for better query optimization
        speedtestQuery = speedtestQuery.Where(s =>
            (serviceProviderIds == null || serviceProviderIds.Count == 0 || serviceProviderIds.Contains(s.ServiceProviderId)) &&
            (providerIds == null || providerIds.Count == 0 || (s.ServiceProvider != null && providerIds.Contains(s.ServiceProvider.ProviderId))) &&
            (serviceIds == null || serviceIds.Count == 0 || (s.ServiceProvider != null && serviceIds.Contains(s.ServiceProvider.ServiceId))) &&
            (generationIds == null || generationIds.Count == 0 || (s.GenerationId.HasValue && generationIds.Contains(s.GenerationId.Value))) &&
            (technologyIds == null || technologyIds.Count == 0 || (s.TechnologyId.HasValue && technologyIds.Contains(s.TechnologyId.Value))) &&
            (string.IsNullOrEmpty(signalQuality) || s.SignalQuality == signalQuality) &&
            (!minDownloadSpeed.HasValue || s.DownloadSpeed >= minDownloadSpeed.Value) &&
            (!maxDownloadSpeed.HasValue || s.DownloadSpeed <= maxDownloadSpeed.Value) &&
            (!minUploadSpeed.HasValue || s.UploadSpeed >= minUploadSpeed.Value) &&
            (!maxUploadSpeed.HasValue || s.UploadSpeed <= maxUploadSpeed.Value) &&
            (!startDate.HasValue || s.DateTime >= startDate.Value) &&
            (!endDate.HasValue || s.DateTime <= endDate.Value) &&
            (!isNoBroadbandCoverage.HasValue || s.IsNoBroadbandCoverage == isNoBroadbandCoverage.Value));

        // Project to DTO - ordered by DateTime descending (most recent first)
        var speedtestDtos = speedtestQuery
            .OrderByDescending(s => s.DateTime)
            .Select(s => new SpeedtestDto
            {
                Id = s.Id,
                DateTime = s.DateTime,
                Ping = s.Ping,
                Jitter = s.Jitter,
                RSSI = s.RSSI,
                SignalQuality = s.SignalQuality,
                UploadSpeed = s.UploadSpeed,
                DownloadSpeed = s.DownloadSpeed,
                Latitude = s.Location.Coordinate.Y,
                Longitude = s.Location.Coordinate.X,
                PhoneModel = s.PhoneModel,
                MCC = s.MCC,
                MNC = s.MNC,
                TAC = s.TAC,
                CellIdentifier = s.CellIdentifier,
                ServiceProvider = s.ServiceProvider == null ? null : new()
                {
                    Id = s.ServiceProviderId,
                    Service = s.ServiceProvider.Service == null ? null : new()
                    {
                        Id = s.ServiceProvider.ServiceId,
                        Name = s.ServiceProvider.Service.Name,
                        Type = s.ServiceProvider.Service.Type.ToString()
                    },
                    Provider = s.ServiceProvider.Provider == null ? null : new()
                    {
                        Id = s.ServiceProvider.ProviderId,
                        Name = s.ServiceProvider.Provider.Name,
                        Description = s.ServiceProvider.Provider.Description,
                        LicenseNumber = s.ServiceProvider.Provider.LicenseNumber,
                        Color = s.ServiceProvider.Provider.Color
                    },
                },
                Generation = s.Generation == null ? null : new()
                {
                    Id = s.GenerationId ?? Guid.Empty,
                    Name = s.Generation.Name,
                    Description = s.Generation.Description
                },
                Technology = s.Technology == null ? null : new()
                {
                    Id = s.Technology.Id,
                    Standard = s.Technology.Standard,
                    Description = s.Technology.Description,
                    Generation = s.Technology.Generation == null ? null : new()
                    {
                        Id = s.Technology.Generation.Id,
                        Name = s.Technology.Generation.Name,
                        Description = s.Technology.Generation.Description
                    }
                },
                Address = s.Address,
                IsFileUploaded = s.IsFileUploaded,
                IsFixedBroadband = s.IsFixedBroadband,
                IsNoBroadbandCoverage = s.IsNoBroadbandCoverage
            });

        return await _speedtestRepository.PaginateAsync(speedtestDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
