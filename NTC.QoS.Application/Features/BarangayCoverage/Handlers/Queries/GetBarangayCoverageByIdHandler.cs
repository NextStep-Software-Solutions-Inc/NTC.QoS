using MediatR;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Handlers.Queries;

public class GetBarangayCoverageByIdHandler(
    IBarangayCoverageRepository barangayCoverageRepository)
    : IRequestHandler<GetBarangayCoverageByIdRequest, ApiResponse<BarangayCoverageDto>>
{
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;

    public async Task<ApiResponse<BarangayCoverageDto>> Handle(GetBarangayCoverageByIdRequest request, CancellationToken cancellationToken)
    {
        var coverage = await _barangayCoverageRepository.GetByIdWithRelationshipsAsync(request.Id, cancellationToken);
        
        if (coverage is null)
        {
            return new ApiResponse<BarangayCoverageDto>
            {
                Succeeded = false,
                Message = "Coverage data not found."
            };
        }

        return new ApiResponse<BarangayCoverageDto>
        {
            Data = new BarangayCoverageDto
            {
                Id = coverage.Id,
                BarangayId = coverage.BarangayId,
                BarangayName = coverage.Barangay.Name,
                BarangayCode = coverage.Barangay.Code,
                WiredFtthPercentage = coverage.WiredFtthPercentage,
                WirelessPercentage = coverage.WirelessPercentage,
                UpdatedAt = coverage.UpdatedAt,
                UpdatedBy = coverage.UpdatedBy
            },
            Succeeded = true,
            Message = "Coverage data retrieved successfully."
        };
    }
}
