using MediatR;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Handlers.Queries;

public class GetAllBarangayCoveragesHandler(
    IBarangayCoverageRepository barangayCoverageRepository)
    : IRequestHandler<GetAllBarangayCoveragesRequest, ApiResponse<List<BarangayCoverageLightDto>>>
{
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;

    public async Task<ApiResponse<List<BarangayCoverageLightDto>>> Handle(GetAllBarangayCoveragesRequest request, CancellationToken cancellationToken)
    {
        var coverageList = await _barangayCoverageRepository.GetAllWithFiltersAsync(
            request.RegionIds,
            request.ProvinceIds,
            request.MunicipalityIds,
            request.BarangayIds,
            cancellationToken);
        
        var coverageDtos = coverageList.Select(c => new BarangayCoverageLightDto
        {
            Id = c.Id,
            BarangayId = c.BarangayId,
            WiredFtthPercentage = c.WiredFtthPercentage,
            WirelessPercentage = c.WirelessPercentage,
            UpdatedAt = c.UpdatedAt,
            UpdatedBy = c.UpdatedBy
        }).ToList();

        return new ApiResponse<List<BarangayCoverageLightDto>>
        {
            Data = coverageDtos,
            Succeeded = true,
            Message = $"Retrieved {coverageDtos.Count} coverage records."
        };
    }
}
