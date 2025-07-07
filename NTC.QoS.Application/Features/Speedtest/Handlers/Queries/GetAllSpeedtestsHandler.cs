using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Queries;

public class GetAllSpeedtestsHandler(ISpeedtestRepository speedtestRepository,
    IRepository<Domain.Entities.Barangay> barangayRepository,
    IRepository<Domain.Entities.Municipality> municipalityRepository,
    IRepository<Domain.Entities.Province> provinceRepository,
    IRepository<Domain.Entities.Region> regionRepository) : IRequestHandler<GetAllSpeedtestsRequest, PaginatedResponse<SpeedtestDto>>
{
    private readonly ISpeedtestRepository _speedtestRepository = speedtestRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository = barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository = municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository = provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;

    public async Task<PaginatedResponse<SpeedtestDto>> Handle(GetAllSpeedtestsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.Speedtest> speedtestQuery = _speedtestRepository.Query();

        Point locationPoint = GeometryHelper.CreatePoint(request.Coordinate);

        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchExpression = SearchHelper.BuildNullableKeywordSearchExpression<Domain.Entities.Speedtest>(
                request.Search,
                s => s.PhoneModel,
                s => s.MCC,
                s => s.MNC,
                s => s.TAC
            );
            speedtestQuery = speedtestQuery.Where(searchExpression);
        }

        speedtestQuery = speedtestQuery
            .Where(s => request.ServiceProviderIds == null || (request.ServiceProviderIds.Count > 0 && request.ServiceProviderIds.Contains(s.ServiceProviderId)))
            .Where(s => request.ProviderIds == null || (request.ProviderIds.Count > 0 && (s.ServiceProvider != null && request.ProviderIds.Contains(s.ServiceProvider.ProviderId))))
            .Where(s => request.ServiceIds == null || (request.ServiceIds.Count > 0 && (s.ServiceProvider != null && request.ServiceIds.Contains(s.ServiceProvider.ServiceId))))
            .Where(s => request.GenerationIds == null || (request.GenerationIds.Count > 0 && request.GenerationIds.Contains(s.GenerationId)))
            .Where(s => request.SignalQuality == null || s.SignalQuality == EnumHelper<SignalQualityEnum>.ParseUserInput(request.SignalQuality))
            .Where(s => locationPoint.IsValid && s.Location.IsWithinDistance(locationPoint, request.Distance));

        if (request.BarangayIds is { Count: > 0 })
        {
            speedtestQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.BarangayIds, _barangayRepository, speedtestQuery, cancellationToken);
        }
        else if (request.MunicipalityIds is { Count: > 0 })
        {
            speedtestQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.MunicipalityIds, _municipalityRepository, speedtestQuery, cancellationToken);
        }
        else if (request.ProvinceIds is { Count: > 0 })
        {
            speedtestQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.ProvinceIds, _provinceRepository, speedtestQuery, cancellationToken);
        }
        else if (request.RegionIds is { Count: > 0 })
        {
            speedtestQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.RegionIds, _regionRepository, speedtestQuery, cancellationToken);
        }
        speedtestQuery = speedtestQuery.OrderBy(s => s.Location.Distance(locationPoint));

        var speedtestDtos = speedtestQuery.Select(s => new SpeedtestDto
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
            ServiceProvider = new()
            {
                Id = s.ServiceProviderId,
                Service = new()
                {
                    Id = s.ServiceProvider.ServiceId,
                    Name = s.ServiceProvider.Service.Name,
                    Type = s.ServiceProvider.Service.Type.ToString()
                },
                Provider = new()
                {
                    Id = s.ServiceProvider.ProviderId,
                    Name = s.ServiceProvider.Provider.Name,
                    Description = s.ServiceProvider.Provider.Description,
                    LicenseNumber = s.ServiceProvider.Provider.LicenseNumber,
                    Color = s.ServiceProvider.Provider.Color
                },
            },
            Generation = new()
            {
                Id = s.GenerationId,
                Name = s.Generation.Name,
                Description = s.Generation.Description
            },
            Address = s.Address,
            IsFileUploaded = s.IsFileUploaded,
            Distance = Math.Round(s.Location.Distance(locationPoint), 2)
        });

        return await _speedtestRepository.PaginateAsync(speedtestDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
