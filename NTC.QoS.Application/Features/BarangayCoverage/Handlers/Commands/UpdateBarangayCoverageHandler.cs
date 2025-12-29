using MediatR;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Handlers.Commands;

public class UpdateBarangayCoverageHandler(
    IBarangayCoverageRepository barangayCoverageRepository)
    : IRequestHandler<UpdateBarangayCoverageRequest, ApiResponse<BarangayCoverageDto>>
{
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;

    public async Task<ApiResponse<BarangayCoverageDto>> Handle(UpdateBarangayCoverageRequest request, CancellationToken cancellationToken)
    {
        // Get existing coverage
        var existingCoverage = await _barangayCoverageRepository.GetByBarangayIdAsync(request.BarangayId, cancellationToken);
        
        if (existingCoverage is null)
        {
            return new ApiResponse<BarangayCoverageDto>
            {
                Succeeded = false,
                Message = "Coverage data not found for this barangay. Use CREATE instead."
            };
        }

        // Update coverage values
        existingCoverage.WiredFtthPercentage = request.Coverage.WiredFtthPercentage;
        existingCoverage.WirelessPercentage = request.Coverage.WirelessPercentage;

        _barangayCoverageRepository.Update(existingCoverage);
        await _barangayCoverageRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<BarangayCoverageDto>
        {
            Data = new BarangayCoverageDto
            {
                Id = existingCoverage.Id,
                BarangayId = existingCoverage.BarangayId,
                BarangayName = existingCoverage.Barangay.Name,
                BarangayCode = existingCoverage.Barangay.Code,
                WiredFtthPercentage = existingCoverage.WiredFtthPercentage,
                WirelessPercentage = existingCoverage.WirelessPercentage,
                UpdatedAt = existingCoverage.UpdatedAt,
                UpdatedBy = existingCoverage.UpdatedBy
            },
            Succeeded = true,
            Message = "Barangay coverage updated successfully."
        };
    }
}
