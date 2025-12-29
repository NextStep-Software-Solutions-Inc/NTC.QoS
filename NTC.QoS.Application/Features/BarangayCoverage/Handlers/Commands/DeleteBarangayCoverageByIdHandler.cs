using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.BarangayCoverage.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.BarangayCoverage.Handlers.Commands;

public class DeleteBarangayCoverageByIdHandler(
    IBarangayCoverageRepository barangayCoverageRepository)
    : IRequestHandler<DeleteBarangayCoverageByIdRequest, Unit>
{
    private readonly IBarangayCoverageRepository _barangayCoverageRepository = barangayCoverageRepository;

    public async Task<Unit> Handle(DeleteBarangayCoverageByIdRequest request, CancellationToken cancellationToken)
    {
        // Check if coverage exists using Any
        var coverageQuery = _barangayCoverageRepository.Query(bc => bc.Id == request.Id);
        var exists = await coverageQuery.AnyAsync(cancellationToken);
        
        if (!exists)
        {
            throw new KeyNotFoundException($"Coverage data with ID {request.Id} not found.");
        }

        // Get and delete the coverage
        var coverage = await _barangayCoverageRepository.FirstOrDefaultAsync(coverageQuery, cancellationToken);
        
        if (coverage is null)
        {
            throw new KeyNotFoundException($"Coverage data with ID {request.Id} not found.");
        }

        _barangayCoverageRepository.Delete(coverage);
        await _barangayCoverageRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
