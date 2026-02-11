using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Commands;

/// <summary>
/// Handler for creating a new NAP.
/// Validation is performed by CreateNAPRequestValidator via FluentValidation pipeline.
/// </summary>
public class CreateNAPHandler(INAPRepository repository, IServiceProviderRepository serviceProviderRepository) : IRequestHandler<CreateNAPRequest, ApiResponse<NAPDto>>
{
    private readonly INAPRepository _repository = repository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;

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

        // Fetch only required ServiceProvider details using projection for optimal performance
        var providerDetailsQuery = _serviceProviderRepository
            .QueryAsNoTracking(sp => sp.Id == request.NAP.ServiceProviderId)
            .Select(sp => new
            {
                Name = sp.Provider != null ? sp.Provider.Name : "",
                Color = sp.Provider != null ? sp.Provider.Color : ""
            });
        var providerDetails = await _serviceProviderRepository.FirstOrDefaultAsync(providerDetailsQuery, cancellationToken);

        // Return NAP with ServiceProvider details
        return new ApiResponse<NAPDto>
        {
            Data = new NAPDto
            {
                Id = NAP.Id,
                ISP = providerDetails?.Name ?? "",
                ISPColor = providerDetails?.Color ?? "",
                Lat = NAP.Location.Coordinate.Y,
                Lng = NAP.Location.Coordinate.X,
                Dist = 0
            },
            Message = "NAP created successfully.",
            Succeeded = true
        };
    }
}
