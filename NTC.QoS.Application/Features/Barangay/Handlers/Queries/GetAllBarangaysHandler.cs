using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Barangay.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Barangay;

namespace NTC.QoS.Application.Features.Barangay.Handlers.Queries;

public class GetAllBarangaysHandler(
    IBarangayRepository barangayRepository,
    IBarangayCoverageRepository barangayCoverageRepository) 
    : IRequestHandler<GetAllBarangaysRequest, PaginatedResponse<BarangayDto>>
{
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;

    public async Task<PaginatedResponse<BarangayDto>> Handle(GetAllBarangaysRequest request, CancellationToken cancellationToken)
    {
        var allBarangaysQuery = _barangayRepository.Query();

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
            
            var coveredBarangayIds = coverageQuery.Select(bc => bc.BarangayId);
            allBarangaysQuery = allBarangaysQuery.Where(b => coveredBarangayIds.Contains(b.Id));
        }

        if (request.MunicipalityIds is not null && request.MunicipalityIds.Length > 0)
        {
            allBarangaysQuery = allBarangaysQuery.Where(b => request.MunicipalityIds.Contains(b.MunicipalityId));
        }

        if (request.RegionIds is not null && request.RegionIds.Length > 0)
        {
            allBarangaysQuery = allBarangaysQuery.Where(b => request.RegionIds.Contains(b.Municipality.Province.RegionId));
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            allBarangaysQuery = _barangayRepository.SearchByName(allBarangaysQuery, request.Name);
        }

        if (!string.IsNullOrEmpty(request.Municipality))
        {
            allBarangaysQuery = _barangayRepository.SearchByMunicipalityName(allBarangaysQuery, request.Municipality);
        }

        if (!string.IsNullOrEmpty(request.Province))
        {
            allBarangaysQuery = _barangayRepository.SearchByProvinceName(allBarangaysQuery, request.Province);
        }

        var barangayDtosQuery = allBarangaysQuery
            .OrderBy(b => b.Name)
            .Select(b => new BarangayDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Municipality = new()
                {
                    Id = b.MunicipalityId,
                    Name = b.Municipality.Name,
                    IsCity = b.Municipality.IsCity,
                    Description = b.Municipality.Description,
                    Province = new()
                    {
                        Id = b.Municipality.ProvinceId,
                        Name = b.Municipality.Province.Name,
                        Description = b.Municipality.Province.Description,
                        Region = new()
                        {
                            Id = b.Municipality.Province.Region.Id,
                            Name = b.Municipality.Province.Region.Name,
                            Description = b.Municipality.Province.Region.Description
                        }
                    }
                },
                Code = b.Code ?? string.Empty,
                Area = b.Area,
            });

        return await _barangayRepository.PaginateAsync(barangayDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
