using MediatR;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Handlers.Queries;

public class GetBarangayCoverageByRegionHandler(
    IBarangayCoverageRepository barangayCoverageRepository,
    IRegionRepository regionRepository)
    : IRequestHandler<GetBarangayCoverageByRegionRequest, ApiResponse<List<BarangayCoverageDto>>>
{
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;
    private readonly IRegionRepository _regionRepository = regionRepository;

    public async Task<ApiResponse<List<BarangayCoverageDto>>> Handle(GetBarangayCoverageByRegionRequest request, CancellationToken cancellationToken)
    {
        // Validate that the region exists
        var regionQuery = _regionRepository.Query(r => r.Id == request.RegionId);
        var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);
        
        if (region is null)
        {
            return new ApiResponse<List<BarangayCoverageDto>>
            {
                Succeeded = false,
                Message = "Region not found."
            };
        }

        var coverageList = await _barangayCoverageRepository.GetByRegionIdAsync(request.RegionId, cancellationToken);
        
        var coverageDtos = coverageList.Select(c => new BarangayCoverageDto
        {
            Id = c.Id,
            BarangayId = c.BarangayId,
            BarangayName = c.Barangay.Name,
            BarangayCode = c.Barangay.Code,
            WiredFtthPercentage = c.WiredFtthPercentage,
            WirelessPercentage = c.WirelessPercentage,
            UpdatedAt = c.UpdatedAt,
            UpdatedBy = c.UpdatedBy
        }).ToList();

        return new ApiResponse<List<BarangayCoverageDto>>
        {
            Data = coverageDtos,
            Succeeded = true,
            Message = $"Retrieved {coverageDtos.Count} coverage records for region {region.Name}."
        };
    }
}
