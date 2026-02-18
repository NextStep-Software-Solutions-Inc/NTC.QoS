using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.NAP.Handlers.Commands;

public class DeleteNAPsByIdsHandler(IRepository<Domain.Aggregates.NAP> repository, IBarangayRepository barangayRepository, IBackgroundJobService backgroundJobService, ILogger<DeleteNAPsByIdsHandler> logger) : IRequestHandler<DeleteNAPsByIdsRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Aggregates.NAP> _repository = repository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IBackgroundJobService _backgroundJobService = backgroundJobService;
    private readonly ILogger<DeleteNAPsByIdsHandler> _logger = logger;

    public async Task<ApiResponse<bool>> Handle(DeleteNAPsByIdsRequest request, CancellationToken cancellationToken)
    {
        // Get NAPs with their locations before deletion to trigger assessment
        var napsToDelete = await _repository.Query(n => request.Ids.Contains(n.Id))
            .Select(n => new { n.Id, n.Location })
            .ToListAsync(cancellationToken);

        if (napsToDelete.Count == 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified NAPs exist or are already deleted."
            };
        }

        var napQuery = _repository.Query(n => request.Ids.Contains(n.Id));
        var deletedCount = await _repository.ExecuteHardDeleteAsync(napQuery, cancellationToken);

        if (deletedCount is 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified NAPs exist or are already deleted."
            };
        }

        // Trigger connectivity assessment generation for affected regions
        // If region ID provided in request, use it directly (most efficient)
        if (request.RegionId.HasValue)
        {
            _logger.LogInformation("Triggering connectivity assessment for region {RegionId} after deleting {Count} NAPs", 
                request.RegionId.Value, deletedCount);
            _backgroundJobService.Enqueue<IGenerateConnectivityAssessmentJob>(
                job => job.ExecuteForRegionAsync(request.RegionId.Value, cancellationToken));
        }
        else
        {
            // Fallback to spatial lookup if no region provided
            _logger.LogDebug("No region ID provided in request, performing spatial lookups for {Count} deleted NAPs", napsToDelete.Count);
            var regionIds = new HashSet<Guid>();
            foreach (var nap in napsToDelete)
            {
                if (nap.Location != null)
                {
                    var barangay = await _barangayRepository.FindBarangayByLocationWithHierarchyAsync(nap.Location, cancellationToken);
                    if (barangay?.Municipality?.Province?.RegionId != null)
                    {
                        regionIds.Add(barangay.Municipality.Province.RegionId);
                    }
                }
            }

            // Trigger assessment once per affected region
            _logger.LogInformation("Triggering connectivity assessments for {RegionCount} regions after NAP deletion", regionIds.Count);
            foreach (var regionId in regionIds)
            {
                _backgroundJobService.Enqueue<IGenerateConnectivityAssessmentJob>(
                    job => job.ExecuteForRegionAsync(regionId, cancellationToken));
            }
            
            if (regionIds.Count == 0)
            {
                _logger.LogWarning("Could not determine region for any of the {Count} deleted NAPs. Assessment not triggered.", napsToDelete.Count);
            }
        }

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = $"{deletedCount} NAPs deleted successfully."
        };
    }
}
