using MediatR;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Handlers.Commands;

public class CreateBarangayCoverageHandler(
    IBarangayCoverageRepository barangayCoverageRepository,
    IBarangayRepository barangayRepository)
    : IRequestHandler<CreateBarangayCoverageRequest, ApiResponse<BarangayCoverageDto>>
{
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;

    public async Task<ApiResponse<BarangayCoverageDto>> Handle(CreateBarangayCoverageRequest request, CancellationToken cancellationToken)
    {
        // Validate that the barangay exists
        var barangayQuery = _barangayRepository.Query(b => b.Id == request.Coverage.BarangayId);
        var barangay = await _barangayRepository.FirstOrDefaultAsync(barangayQuery, cancellationToken);
        
        if (barangay is null)
        {
            return new ApiResponse<BarangayCoverageDto>
            {
                Succeeded = false,
                Message = "Barangay not found."
            };
        }

        // Check if coverage already exists for this barangay
        var existingCoverage = await _barangayCoverageRepository.GetByBarangayIdAsync(request.Coverage.BarangayId, cancellationToken);
        
        if (existingCoverage is not null)
        {
            return new ApiResponse<BarangayCoverageDto>
            {
                Succeeded = false,
                Message = "Coverage data already exists for this barangay. Use UPDATE instead."
            };
        }

        // Create new coverage entry
        var newCoverage = new Domain.Entities.BarangayCoverage
        {
            BarangayId = request.Coverage.BarangayId,
            WiredFtthPercentage = request.Coverage.WiredFtthPercentage,
            WirelessPercentage = request.Coverage.WirelessPercentage
        };

        _barangayCoverageRepository.Add(newCoverage);
        await _barangayCoverageRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<BarangayCoverageDto>
        {
            Data = new BarangayCoverageDto
            {
                Id = newCoverage.Id,
                BarangayId = newCoverage.BarangayId,
                BarangayName = barangay.Name,
                BarangayCode = barangay.Code,
                WiredFtthPercentage = newCoverage.WiredFtthPercentage,
                WirelessPercentage = newCoverage.WirelessPercentage,
                UpdatedAt = newCoverage.UpdatedAt,
                UpdatedBy = newCoverage.UpdatedBy
            },
            Succeeded = true,
            Message = "Barangay coverage created successfully."
        };
    }
}
