using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Commands;

public class DeleteSpeedtestsByIdsHandler(IRepository<Domain.Entities.Speedtest> repository, IBarangayRepository barangayRepository, IBackgroundJobService backgroundJobService, ILogger<DeleteSpeedtestsByIdsHandler> logger) : IRequestHandler<DeleteSpeedtestsByIdsRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Speedtest> _repository = repository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IBackgroundJobService _backgroundJobService = backgroundJobService;
    private readonly ILogger<DeleteSpeedtestsByIdsHandler> _logger = logger;

    public async Task<ApiResponse<bool>> Handle(DeleteSpeedtestsByIdsRequest request,
            CancellationToken cancellationToken)
    {
        // Get speedtests with their locations before deletion to trigger assessment
        var speedtestsToDelete = await _repository.Query(st => request.Ids.Contains(st.Id))
            .Select(st => new { st.Id, st.Location })
            .ToListAsync(cancellationToken);

        if (speedtestsToDelete.Count == 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified speedtest/s exist or are already deleted."
            };
        }

        var speedtestQuery = _repository.Query(st => request.Ids.Contains(st.Id));
        var deletedCount = await _repository.ExecuteHardDeleteAsync(speedtestQuery, cancellationToken);

        if (deletedCount is 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified speedtest/s exist or are already deleted."
            };
        }

        // Trigger connectivity assessment generation for affected regions
        // If region ID provided in request, use it directly (most efficient)
        if (request.RegionId.HasValue)
        {
            _logger.LogInformation("Triggering connectivity assessment for region {RegionId} after deleting {Count} speedtests", 
                request.RegionId.Value, deletedCount);
            _backgroundJobService.Enqueue<IGenerateConnectivityAssessmentJob>(
                job => job.ExecuteForRegionAsync(request.RegionId.Value, cancellationToken));
        }
        else
        {
            // Fallback to spatial lookup if no region provided
            _logger.LogDebug("No region ID provided in request, performing spatial lookups for {Count} deleted speedtests", speedtestsToDelete.Count);
            var regionIds = new HashSet<Guid>();
            foreach (var speedtest in speedtestsToDelete)
            {
                if (speedtest.Location != null)
                {
                    var barangay = await _barangayRepository.FindBarangayByLocationWithHierarchyAsync(speedtest.Location, cancellationToken);
                    if (barangay?.Municipality?.Province?.RegionId != null)
                    {
                        regionIds.Add(barangay.Municipality.Province.RegionId);
                    }
                }
            }

            // Trigger assessment once per affected region
            _logger.LogInformation("Triggering connectivity assessments for {RegionCount} regions after speedtest deletion", regionIds.Count);
            foreach (var regionId in regionIds)
            {
                _backgroundJobService.Enqueue<IGenerateConnectivityAssessmentJob>(
                    job => job.ExecuteForRegionAsync(regionId, cancellationToken));
            }
            
            if (regionIds.Count == 0)
            {
                _logger.LogWarning("Could not determine region for any of the {Count} deleted speedtests. Assessment not triggered.", speedtestsToDelete.Count);
            }
        }

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"{deletedCount} speedtest/s record deleted successfully."
        };
    }
}