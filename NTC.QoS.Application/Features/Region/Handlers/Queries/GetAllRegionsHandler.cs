using MediatR;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Queries;

public class GetAllRegionsHandler(
    IRegionRepository repository,
    IBarangayCoverageRepository barangayCoverageRepository) 
    : IRequestHandler<GetAllRegionsRequest, PaginatedResponse<RegionDto>>
{
    private readonly IRegionRepository _repository = repository;
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;

    public async Task<PaginatedResponse<RegionDto>> Handle(GetAllRegionsRequest request, CancellationToken cancellationToken)
    {
        var allRegions = _repository.Query();

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
            
            var coveredRegionIds = coverageQuery
                .Select(bc => bc.Barangay.Municipality.Province.RegionId)
                .Distinct();
            allRegions = allRegions.Where(r => coveredRegionIds.Contains(r.Id));
        }

        var regionDtos = allRegions.OrderBy(r => r.Name).Select(r => new RegionDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Code = r.Code,
            Area = r.Area,
        });

        return await _repository.PaginateAsync(regionDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
