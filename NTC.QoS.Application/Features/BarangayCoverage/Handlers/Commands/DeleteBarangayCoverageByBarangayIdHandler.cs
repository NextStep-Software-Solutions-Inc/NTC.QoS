using MediatR;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.BarangayCoverage.Handlers.Commands;

public class DeleteBarangayCoverageByBarangayIdHandler(
    IBarangayCoverageRepository barangayCoverageRepository)
    : IRequestHandler<DeleteBarangayCoverageByBarangayIdRequest, ApiResponse<bool>>
{
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;

    public async Task<ApiResponse<bool>> Handle(DeleteBarangayCoverageByBarangayIdRequest request, CancellationToken cancellationToken)
    {
        var coverage = await _barangayCoverageRepository.GetByBarangayIdAsync(request.BarangayId, cancellationToken);

        if (coverage is null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = "Coverage data not found for this barangay."
            };
        }

        _barangayCoverageRepository.Delete(coverage);
        await _barangayCoverageRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = "Barangay coverage deleted successfully."
        };
    }
}
