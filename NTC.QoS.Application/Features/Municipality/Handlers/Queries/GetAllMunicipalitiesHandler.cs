using MediatR;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Queries;

public class GetAllMunicipalitiesHandler(
    IMunicipalityRepository municipalityRepository,
    IBarangayCoverageRepository barangayCoverageRepository) 
    : IRequestHandler<GetAllMunicipalitiesRequest, PaginatedResponse<MunicipalityDto>>
{
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;

    public async Task<PaginatedResponse<MunicipalityDto>> Handle(GetAllMunicipalitiesRequest request, CancellationToken cancellationToken)
    {
        var allMunicipalities = _municipalityRepository.Query();

        // Filter by coverage status if requested
        if (request.IsWiredCovered.HasValue || request.IsWirelessCovered.HasValue)
        {
            var coverageQuery = _barangayCoverageRepository.Query();
            
            if (request.IsWiredCovered.HasValue)
            {
                if (request.IsWiredCovered.Value)
                {
                    coverageQuery = coverageQuery.Where(bc => bc.WiredFtthPercentage == 100);
                }
                else
                {
                    coverageQuery = coverageQuery.Where(bc => bc.WiredFtthPercentage < 100);
                }
            }
            
            if (request.IsWirelessCovered.HasValue)
            {
                if (request.IsWirelessCovered.Value)
                {
                    coverageQuery = coverageQuery.Where(bc => bc.WirelessPercentage == 100);
                }
                else
                {
                    coverageQuery = coverageQuery.Where(bc => bc.WirelessPercentage < 100);
                }
            }
            
            var coveredMunicipalityIds = coverageQuery
                .Select(bc => bc.Barangay.MunicipalityId)
                .Distinct();
            allMunicipalities = allMunicipalities.Where(m => coveredMunicipalityIds.Contains(m.Id));
        }

        // Filter by province(s) - ProvinceIds takes precedence over deprecated ProvinceId
        if (request.ProvinceIds is not null && request.ProvinceIds.Length > 0)
        {
            allMunicipalities = allMunicipalities.Where(m => request.ProvinceIds.Contains(m.ProvinceId));
        }
#pragma warning disable CS0618 // Type or member is obsolete
        else if (request.ProvinceId.HasValue)
        {
            allMunicipalities = allMunicipalities.Where(m => m.Province.Id == request.ProvinceId);
        }
#pragma warning restore CS0618 // Type or member is obsolete

        if (request.RegionIds is not null && request.RegionIds.Length > 0)
        {
            allMunicipalities = allMunicipalities.Where(m => m.Province != null && request.RegionIds.Contains(m.Province.RegionId));
        }

        var municipalityDtos = allMunicipalities.OrderBy(m => m.Name).Select(m => new MunicipalityDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            IsCity = m.IsCity,
            Province = new()
            {
                Id = m.ProvinceId,
                Name = m.Province != null ? m.Province.Name : string.Empty,
                Description = m.Province != null ? m.Province.Description : null,
                Region = new()
                {
                    Id = m.Province != null && m.Province.Region != null ? m.Province.Region.Id : Guid.Empty,
                    Name = m.Province != null && m.Province.Region != null ? m.Province.Region.Name : string.Empty,
                    Description = m.Province != null && m.Province.Region != null ? m.Province.Region.Description : null,
                }
            },
            Code = m.Code ?? string.Empty,
            Area = m.Area
        });

        return await _municipalityRepository.PaginateAsync(municipalityDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
