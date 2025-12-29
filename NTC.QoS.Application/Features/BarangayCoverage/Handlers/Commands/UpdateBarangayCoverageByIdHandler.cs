using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.BarangayCoverage.Handlers.Commands;

public class UpdateBarangayCoverageByIdHandler(
    IBarangayCoverageRepository barangayCoverageRepository)
    : IRequestHandler<UpdateBarangayCoverageByIdRequest, Unit>
{
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;

    public async Task<Unit> Handle(UpdateBarangayCoverageByIdRequest request, CancellationToken cancellationToken)
    {
        // Check if coverage exists using Any
        var coverageQuery = _barangayCoverageRepository.Query(bc => bc.Id == request.Id);
        var exists = await coverageQuery.AnyAsync(cancellationToken);
        
        if (!exists)
        {
            throw new KeyNotFoundException($"Coverage data with ID {request.Id} not found.");
        }

        // Get the existing coverage to update
        var existingCoverage = await _barangayCoverageRepository.FirstOrDefaultAsync(coverageQuery, cancellationToken);
        
        if (existingCoverage is null)
        {
            throw new KeyNotFoundException($"Coverage data with ID {request.Id} not found.");
        }

        // Update coverage values
        existingCoverage.WiredFtthPercentage = request.Coverage.WiredFtthPercentage;
        existingCoverage.WirelessPercentage = request.Coverage.WirelessPercentage;

        _barangayCoverageRepository.Update(existingCoverage);
        await _barangayCoverageRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
