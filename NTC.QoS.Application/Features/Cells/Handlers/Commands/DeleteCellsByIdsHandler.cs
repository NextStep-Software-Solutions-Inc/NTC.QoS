using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class DeleteCellsByIdsHandler(ICellRepository repository, IBarangayRepository barangayRepository, IBackgroundJobService backgroundJobService, ILogger<DeleteCellsByIdsHandler> logger) : IRequestHandler<DeleteCellsByIdsRequest, ApiResponse<bool>>
{
    private readonly ICellRepository _repository = repository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IBackgroundJobService _backgroundJobService = backgroundJobService;
    private readonly ILogger<DeleteCellsByIdsHandler> _logger = logger;

    public async Task<ApiResponse<bool>> Handle(DeleteCellsByIdsRequest request, CancellationToken cancellationToken)
    {
        // Get cells with their locations before deletion to trigger assessment
        var cellsToDelete = await _repository.Query(c => request.Ids.Contains(c.Id))
            .Select(c => new { c.Id, c.Location })
            .ToListAsync(cancellationToken);

        if (cellsToDelete.Count == 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified cell/s exist or are already deleted.",
            };
        }

        var cellsToDeleteQuery = _repository.Query(c => request.Ids.Contains(c.Id));
        var deletedCount = await _repository.ExecuteHardDeleteAsync(cellsToDeleteQuery, cancellationToken);

        if (deletedCount is 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified cell/s exist or are already deleted.",
            };
        }

        // Trigger connectivity assessment generation for affected regions
        // If region ID provided in request, use it directly (most efficient)
        if (request.RegionId.HasValue)
        {
            _logger.LogInformation("Triggering connectivity assessment for region {RegionId} after deleting {Count} cells", 
                request.RegionId.Value, deletedCount);
            _backgroundJobService.Enqueue<IGenerateConnectivityAssessmentJob>(
                job => job.ExecuteForRegionAsync(request.RegionId.Value, cancellationToken));
        }
        else
        {
            // Fallback to spatial lookup if no region provided
            // OPTIMIZATION: Deduplicate locations to minimize spatial queries (avoid N+1 pattern)
            var distinctLocations = cellsToDelete
                .Where(c => c.Location != null)
                .Select(c => c.Location!)
                .Distinct()
                .ToList();

            if (distinctLocations.Count > 50)
            {
                _logger.LogWarning("Large batch delete of {Count} cells across {LocationCount} distinct locations without region ID. " +
                    "Consider providing RegionId parameter for better performance.", 
                    cellsToDelete.Count, distinctLocations.Count);
            }

            _logger.LogDebug("Performing spatial lookups for {LocationCount} distinct locations from {TotalCount} deleted cells", 
                distinctLocations.Count, cellsToDelete.Count);

            var regionIds = new HashSet<Guid>();
            foreach (var location in distinctLocations)
            {
                var barangay = await _barangayRepository.FindBarangayByLocationWithHierarchyAsync(location, cancellationToken);
                if (barangay?.Municipality?.Province?.RegionId != null)
                {
                    regionIds.Add(barangay.Municipality.Province.RegionId);
                }
            }

            // Trigger assessment once per affected region
            _logger.LogInformation("Triggering connectivity assessments for {RegionCount} regions after cell deletion " +
                "({LocationCount} locations checked, {TotalCount} cells deleted)", 
                regionIds.Count, distinctLocations.Count, deletedCount);
            
            foreach (var regionId in regionIds)
            {
                _backgroundJobService.Enqueue<IGenerateConnectivityAssessmentJob>(
                    job => job.ExecuteForRegionAsync(regionId, cancellationToken));
            }
            
            if (regionIds.Count == 0)
            {
                _logger.LogWarning("Could not determine region for any of the {Count} deleted cells. Assessment not triggered.", cellsToDelete.Count);
            }
        }

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Message = $"{deletedCount} cell/s deleted successfully."
        };
    }
}
