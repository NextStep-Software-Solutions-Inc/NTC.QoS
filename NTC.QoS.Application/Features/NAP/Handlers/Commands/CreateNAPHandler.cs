using MediatR;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Commands;

/// <summary>
/// Handler for creating a new NAP.
/// Input validation is performed by CreateNAPRequestValidator via FluentValidation pipeline.
/// Business rule validation (e.g., duplicate checking) is performed within this handler.
/// </summary>
public class CreateNAPHandler(INAPRepository repository, IServiceProviderRepository serviceProviderRepository, IBarangayRepository barangayRepository, IBackgroundJobService backgroundJobService, ILogger<CreateNAPHandler> logger) : IRequestHandler<CreateNAPRequest, ApiResponse<NAPDto>>
{
    private readonly INAPRepository _repository = repository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IBarangayRepository _barangayRepository = barangayRepository;
    private readonly IBackgroundJobService _backgroundJobService = backgroundJobService;
    private readonly ILogger<CreateNAPHandler> _logger = logger;

    public async Task<ApiResponse<NAPDto>> Handle(CreateNAPRequest request, CancellationToken cancellationToken)
    {
        // All validation is handled by CreateNAPRequestValidator via FluentValidation pipeline
        // Create point geometry
        Point location = GeometryHelper.CreatePoint(x: request.NAP.Longitude, y: request.NAP.Latitude);

        // Check for duplicates using minimal projection (only fetch Id for existence check)
        var duplicateCheck = _repository.Query()
            .Where(n => n.Location.Equals(location) && n.ServiceProviderId == request.NAP.ServiceProviderId)
            .Select(n => n.Id);
        var duplicateId = await _repository.FirstOrDefaultAsync(duplicateCheck, cancellationToken);

        if (duplicateId != Guid.Empty)
        {
            return new ApiResponse<NAPDto>
            {
                Message = "NAP already exists at this location for this service provider.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Duplicate", ["A NAP with the same location and service provider already exists."] }
                }
            };
        }

        // Create NAP with IsFileUploaded = false (manual CRUD operations)
        var NAP = new Domain.Aggregates.NAP
        {
            ServiceProviderId = request.NAP.ServiceProviderId,
            Location = location,
            Address = request.NAP.Address,
            IsFileUploaded = false, // Always false for manual CRUD operations
        };

        _repository.Add(NAP);
        await _repository.SaveChangesAsync(cancellationToken);

        // Defensive check: Ensure Location was properly set
        if (NAP.Location == null || NAP.Location.Coordinate == null)
        {
            _logger.LogError("NAP {NAPId} was created but Location is null", NAP.Id);
            return new ApiResponse<NAPDto>
            {
                Message = "NAP creation failed due to invalid location data.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Location", ["Failed to set NAP location."] }
                }
            };
        }

        // Store location in local variable for null-safe usage (helps compiler flow analysis)
        var napLocation = NAP.Location;

        // Trigger connectivity assessment generation after successful NAP creation
        Guid? regionId = request.RegionId;
        
        // If region not provided in request, fallback to spatial lookup
        if (!regionId.HasValue)
        {
            _logger.LogDebug("No region ID provided in request, performing spatial lookup for NAP {NAPId}", NAP.Id);
            var barangay = await _barangayRepository.FindBarangayByLocationWithHierarchyAsync(napLocation, cancellationToken);
            regionId = barangay?.Municipality?.Province?.RegionId;
        }
        
        if (regionId.HasValue)
        {
            _logger.LogInformation("Triggering connectivity assessment for region {RegionId} after NAP creation", regionId.Value);
            _backgroundJobService.Enqueue<IGenerateConnectivityAssessmentJob>(
                job => job.ExecuteForRegionAsync(regionId.Value, cancellationToken));
        }
        else
        {
            _logger.LogWarning("Could not determine region for NAP {NAPId} at location {Location}. Assessment not triggered.", 
                NAP.Id, napLocation.Coordinate);
        }

        // Fetch only required ServiceProvider details using projection for optimal performance
        var providerDetailsQuery = _serviceProviderRepository
            .QueryAsNoTracking(sp => sp.Id == request.NAP.ServiceProviderId)
            .Select(sp => new
            {
                Name = sp.Provider != null ? sp.Provider.Name : null,
                Color = sp.Provider != null ? sp.Provider.Color : null
            });
        var providerDetails = await _serviceProviderRepository.FirstOrDefaultAsync(providerDetailsQuery, cancellationToken);

        // Validate that Provider data exists (data integrity check)
        if (providerDetails == null || string.IsNullOrEmpty(providerDetails.Name))
        {
            return new ApiResponse<NAPDto>
            {
                Message = "Service provider data is missing or incomplete.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "DataIntegrity", ["The associated service provider information could not be retrieved."] }
                }
            };
        }

        // Return NAP with ServiceProvider details
        // Location is guaranteed to be non-null due to earlier check
        return new ApiResponse<NAPDto>
        {
            Data = new NAPDto
            {
                Id = NAP.Id,
                ISP = providerDetails.Name,
                ISPColor = providerDetails.Color ?? "",
                Lat = napLocation.Coordinate.Y,
                Lng = napLocation.Coordinate.X,
                Dist = 0
            },
            Message = "NAP created successfully.",
            Succeeded = true
        };
    }
}
