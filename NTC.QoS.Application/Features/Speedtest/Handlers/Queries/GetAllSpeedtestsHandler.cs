using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Queries;

public class GetAllSpeedtestsHandler(ISpeedtestRepository repository) : IRequestHandler<GetAllSpeedtestsRequest, PaginatedResponse<SpeedtestDto>>
{
    private readonly ISpeedtestRepository _repository = repository;

    public async Task<PaginatedResponse<SpeedtestDto>> Handle(GetAllSpeedtestsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.Speedtest> speedtestQuery = _repository.Query(s => !s.IsDeleted);

        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchExpression = SearchHelper.BuildNullableKeywordSearchExpression<Domain.Entities.Speedtest>(
                request.Search,
                s => s.PhoneModel,
                s => s.MCC,
                s => s.MNC,
                s => s.TAC,
                s => s.Cell!.Name,
                s => s.Cell!.Description,
                s => s.Cell!.Group,
                s => s.Cell!.Zone!.Name,
                s => s.Cell!.Zone!.Barangay!.Name,
                s => s.Cell!.Zone!.Barangay!.Municipality!.Name,
                s => s.Cell!.Zone!.Barangay!.Municipality!.Province!.Name,
                s => s.Cell!.Zone!.Barangay!.Municipality!.Province!.Region!.Name
            );
            speedtestQuery = speedtestQuery.Where(searchExpression);
        }

        speedtestQuery = speedtestQuery
            .Where(s => request.BarangayIds == null || (request.BarangayIds.Count > 0 && request.BarangayIds.Contains(s.BarangayId)))
            .Where(s => request.ServiceProviderIds == null || (request.ServiceProviderIds.Count > 0 && request.ServiceProviderIds.Contains(s.ServiceProviderId)))
            .Where(s => request.ProviderIds == null || (request.ProviderIds.Count > 0 && (s.ServiceProvider != null && request.ProviderIds.Contains(s.ServiceProvider.ProviderId))))
            .Where(s => request.ServiceIds == null || (request.ServiceIds.Count > 0 && (s.ServiceProvider != null && request.ServiceIds.Contains(s.ServiceProvider.ServiceId))))
            .Where(s => request.GenerationIds == null || (request.GenerationIds.Count > 0 && request.GenerationIds.Contains(s.GenerationId)))
            .Where(s => request.CellIds == null || (request.CellIds.Count > 0 && request.CellIds.Contains(s.CellId)))
            .Where(s => request.SignalQuality == null || s.SignalQuality == request.SignalQuality.Value)
            .Where(s => request.FileIds == null || (request.FileIds.Count > 0 && s.FileId != null && request.FileIds.Contains(s.FileId.Value)));

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
            CoorLatitude = s.CoorLatitude,
            CoorLongitude = s.CoorLongitude,
            PhoneModel = s.PhoneModel,
            MCC = s.MCC,
            MNC = s.MNC,
            TAC = s.TAC,
            File = s.File != null? new()
            {
                Id = s.File.Id,
                Name = s.File.Name,
                Path = s.File.Path
            } : null,
            Barangay = s.Barangay != null ? new()
            {
                Id = s.BarangayId,
                Name = s.Barangay.Name,
                Description = s.Barangay.Description,
                Municipality = s.Barangay.Municipality != null ? new()
                {
                    Id = s.Barangay.MunicipalityId,
                    Name = s.Barangay.Municipality.Name,
                    Description = s.Barangay.Municipality.Description,
                    IsCity = s.Barangay.Municipality.IsCity,
                    Province = s.Barangay.Municipality.Province != null ? new()
                    {
                        Id = s.Barangay.Municipality.ProvinceId,
                        Name = s.Barangay.Municipality.Province.Name,
                        Description = s.Barangay.Municipality.Province.Description,
                        Region = s.Barangay.Municipality.Province.Region != null ? new()
                        {
                            Id = s.Barangay.Municipality.Province.RegionId,
                            Name = s.Barangay.Municipality.Province.Region.Name,
                            Description = s.Barangay.Municipality.Province.Region.Description
                        } : null
                    } : null
                } : null
            } : null,
            ServiceProvider = s.ServiceProvider != null ? new()
            {
                Id = s.ServiceProviderId,
                Service = s.ServiceProvider.Service != null ? new()
                {
                    Id = s.ServiceProvider.ServiceId,
                    Name = s.ServiceProvider.Service.Name,
                    Type = s.ServiceProvider.Service.Type.ToString()
                } : null,
                Provider = s.ServiceProvider.Provider != null ? new()
                {
                    Id = s.ServiceProvider.ProviderId,
                    Name = s.ServiceProvider.Provider.Name,
                    Description = s.ServiceProvider.Provider.Description,
                    LicenseNumber = s.ServiceProvider.Provider.LicenseNumber,
                    Color = s.ServiceProvider.Provider.Color
                } : null
            } : null,
            Generation = s.Generation != null ? new()
            {
                Id = s.GenerationId,
                Name = s.Generation.Name,
                Description = s.Generation.Description
            } : null,
            Cell = s.Cell != null ? new()
            {
                Id = s.CellId,
                Name = s.Cell.Name,
                Group = s.Cell.Group,
                File = s.Cell.File != null ? new()
                {
                    Id = s.Cell.File.Id,
                    Name = s.Cell.File.Name,
                    Path = s.Cell.File.Path
                } : null,
                Description = s.Cell.Description,
                Zone = s.Cell.Zone != null ? new()
                {
                    Id = s.Cell.ZoneId,
                    Name = s.Cell.Zone.Name,
                    Barangay = s.Cell.Zone.Barangay != null ? new()
                    {
                        Id = s.Cell.Zone.BarangayId,
                        Name = s.Cell.Zone.Barangay.Name,
                        Description = s.Cell.Zone.Barangay.Description,
                        Municipality = s.Cell.Zone.Barangay.Municipality != null ? new()
                        {
                            Id = s.Cell.Zone.Barangay.MunicipalityId,
                            IsCity = s.Cell.Zone.Barangay.Municipality.IsCity,
                            Name = s.Cell.Zone.Barangay.Municipality.Name,
                            Description = s.Cell.Zone.Barangay.Municipality.Description,
                            Province = s.Cell.Zone.Barangay.Municipality.Province != null ? new()
                            {
                                Id = s.Cell.Zone.Barangay.Municipality.ProvinceId,
                                Name = s.Cell.Zone.Barangay.Municipality.Province.Name,
                                Description = s.Cell.Zone.Barangay.Municipality.Province.Description,
                                Region = s.Cell.Zone.Barangay.Municipality.Province.Region != null ? new()
                                {
                                    Id = s.Cell.Zone.Barangay.Municipality.Province.Region.Id,
                                    Name = s.Cell.Zone.Barangay.Municipality.Province.Region.Name,
                                    Description = s.Cell.Zone.Barangay.Municipality.Province.Region.Description
                                } : null,
                            } : null,
                        } : null,
                    } : null,
                } : null,
                CID = s.Cell.CID,
                LAC = s.Cell.LAC,
                TAC = s.Cell.TAC,
                SiteId = s.Cell.SiteId,
                TowerType = s.Cell.TowerType,
                Elevation = s.Cell.Elevation,
                CoorLatitude = s.Cell.CoorLatitude,
                CoorLongitude = s.Cell.CoorLongitude,
                IsVerified = s.Cell.IsVerified,
                Status = s.Cell.Status.ToString()
            } : null
        });

        var speedtests = await _repository.ToListAsync(speedtestDtos, cancellationToken);

        if (speedtests.Count is 0)
        {
            return new PaginatedResponse<SpeedtestDto>(new List<SpeedtestDto>(), request.PageNumber, request.PageSize)
            {
                Message = "No records found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullSpeedtests", new[] { "No speedtests found." }
                    }
                }
            };
        }
        return new PaginatedResponse<SpeedtestDto>(speedtests, request.PageNumber, request.PageSize)
        {
            Data = speedtests,
            Message = "Speedtests retrieved successfully.",
            Succeeded = true
        };

    }
}
