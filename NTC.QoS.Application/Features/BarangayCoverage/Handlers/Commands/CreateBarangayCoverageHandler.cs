using MediatR;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.BarangayCoverage.Handlers.Commands;

public class CreateBarangayCoverageHandler(
    IBarangayCoverageRepository barangayCoverageRepository,
    IBarangayRepository barangayRepository)
    : IRequestHandler<CreateBarangayCoverageRequest, ApiResponse<Guid>>
{
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;

    public async Task<ApiResponse<Guid>> Handle(CreateBarangayCoverageRequest request, CancellationToken cancellationToken)
    {
        // Validate that the barangay exists
        var barangayQuery = _barangayRepository.Query(b => b.Id == request.Coverage.BarangayId);
        var barangay = await _barangayRepository.FirstOrDefaultAsync(barangayQuery, cancellationToken);
        
        if (barangay is null)
        {
            return new ApiResponse<Guid>
            {
                Succeeded = false,
                Message = "Barangay not found."
            };
        }

        // Check if coverage already exists for this barangay
        var existingCoverageQuery = _barangayCoverageRepository.Query(bc => bc.BarangayId == request.Coverage.BarangayId);
        var existingCoverage = await _barangayCoverageRepository.FirstOrDefaultAsync(existingCoverageQuery, cancellationToken);
        
        if (existingCoverage is not null)
        {
            return new ApiResponse<Guid>
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

        return new ApiResponse<Guid>
        {
            Data = newCoverage.Id,
            Succeeded = true,
            Message = "Barangay coverage created successfully."
        };
    }
}
