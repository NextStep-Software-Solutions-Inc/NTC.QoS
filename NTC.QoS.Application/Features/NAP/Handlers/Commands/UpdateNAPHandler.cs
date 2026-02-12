using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Commands;

/// <summary>
/// Handler for updating an existing NAP.
/// Validation is performed by UpdateNAPRequestValidator via FluentValidation pipeline.
/// </summary>
public class UpdateNAPHandler(INAPRepository repository, IServiceProviderRepository serviceProviderRepository) : IRequestHandler<UpdateNAPRequest, ApiResponse<NAPDto>>
{
    private readonly INAPRepository _repository = repository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

    public async Task<ApiResponse<NAPDto>> Handle(UpdateNAPRequest request, CancellationToken cancellationToken)
    {
        // All validation is handled by UpdateNAPRequestValidator via FluentValidation pipeline
        // Fetch existing NAP (already validated to exist)
        var napQuery = _repository.Query(n => n.Id == request.Id);
        var existingNap = await _repository.FirstOrDefaultAsync(napQuery, cancellationToken);

        // Update only the fields that are provided (partial update/PATCH behavior)
        Point? newLocation = null;

        // Update location if both coordinates are provided
        if (request.NAP.Latitude.HasValue && request.NAP.Longitude.HasValue)
        {
            newLocation = GeometryHelper.CreatePoint(x: request.NAP.Longitude.Value, y: request.NAP.Latitude.Value);
            
            // Check for duplicates at the new location
            var serviceProviderIdForCheck = request.NAP.ServiceProviderId ?? existingNap!.ServiceProviderId;
            var duplicateCheck = _repository.Query()
                .Where(n => n.Id != request.Id 
                         && n.Location.Equals(newLocation) 
                         && n.ServiceProviderId == serviceProviderIdForCheck)
                .Select(n => n.Id);
            var duplicateId = await _repository.FirstOrDefaultAsync(duplicateCheck, cancellationToken);

            if (duplicateId != Guid.Empty)
            {
                return new ApiResponse<NAPDto>
                {
                    Message = "Another NAP already exists at this location for this service provider.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Duplicate", ["A different NAP with the same location and service provider already exists."] }
                    }
                };
            }

            existingNap!.Location = newLocation;
        }

        // Update ServiceProviderId if provided
        if (request.NAP.ServiceProviderId.HasValue)
        {
            var newServiceProviderId = request.NAP.ServiceProviderId.Value;

            // If location was not changed but ServiceProviderId is changing, check for duplicates
            if (newLocation is null && newServiceProviderId != existingNap!.ServiceProviderId)
            {
                var duplicateCheck = _repository.Query()
                    .Where(n => n.Id != request.Id
                             && n.Location.Equals(existingNap.Location)
                             && n.ServiceProviderId == newServiceProviderId)
                    .Select(n => n.Id);
                var duplicateId = await _repository.FirstOrDefaultAsync(duplicateCheck, cancellationToken);

                if (duplicateId != Guid.Empty)
                {
                    return new ApiResponse<NAPDto>
                    {
                        Message = "Another NAP already exists at this location for this service provider.",
                        Succeeded = false,
                        Errors = new Dictionary<string, string[]>
                        {
                            { "Duplicate", ["A different NAP with the same location and service provider already exists."] }
                        }
                    };
                }
            }

            existingNap!.ServiceProviderId = newServiceProviderId;
        }

        // Update Address if provided
        if (request.NAP.Address != null)
        {
            existingNap!.Address = request.NAP.Address;
        }

        // IsFileUploaded remains false for manual CRUD operations
        existingNap!.IsFileUploaded = false;

        _repository.Update(existingNap);
        await _repository.SaveChangesAsync(cancellationToken);

        // Fetch only required ServiceProvider details using projection for optimal performance
        var providerDetailsQuery = _serviceProviderRepository
            .QueryAsNoTracking(sp => sp.Id == existingNap.ServiceProviderId)
            .Select(sp => new
            {
                Name = sp.Provider != null ? sp.Provider.Name : "",
                Color = sp.Provider != null ? sp.Provider.Color : ""
            });
        var providerDetails = await _serviceProviderRepository.FirstOrDefaultAsync(providerDetailsQuery, cancellationToken);

        // Return updated NAP with ServiceProvider details
        return new ApiResponse<NAPDto>
        {
            Data = new NAPDto
            {
                Id = existingNap.Id,
                ISP = providerDetails?.Name ?? "",
                ISPColor = providerDetails?.Color ?? "",
                Lat = existingNap.Location.Coordinate.Y,
                Lng = existingNap.Location.Coordinate.X,
                Dist = 0
            },
            Message = "NAP updated successfully.",
            Succeeded = true
        };
    }
}
