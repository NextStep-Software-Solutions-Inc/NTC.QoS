using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

public class GetAreaBroadbandInfoHandler : IRequestHandler<GetAreaBroadbandInfoRequest, ApiResponse<AreaBroadbandInfoDto>>
{
    private readonly ICellRepository _cellRepository;
    private readonly INAPRepository _napRepository;

    public GetAreaBroadbandInfoHandler(
        ICellRepository cellRepository,
        INAPRepository napRepository)
    {
        _cellRepository = cellRepository;
        _napRepository = napRepository;
    }

    public async Task<ApiResponse<AreaBroadbandInfoDto>> Handle(GetAreaBroadbandInfoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate bounding box
            if (request.BoundingBox == null)
            {
                return new ApiResponse<AreaBroadbandInfoDto>
                {
                    Succeeded = false,
                    Message = "Bounding box is required.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "BoundingBox", ["Bounding box cannot be null."] }
                    }
                };
            }

            var bbox = request.BoundingBox;

            // Validate bounding box coordinates
            if (bbox.MinLatitude >= bbox.MaxLatitude || bbox.MinLongitude >= bbox.MaxLongitude)
            {
                return new ApiResponse<AreaBroadbandInfoDto>
                {
                    Succeeded = false,
                    Message = "Invalid bounding box coordinates.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "BoundingBox", ["Min coordinates must be less than max coordinates."] }
                    }
                };
            }

            // Validate latitude and longitude ranges for practical map usage
            if (!GeoSpatialConstants.IsWithinSupportedRegion(bbox.MinLatitude, bbox.MinLongitude) ||
                !GeoSpatialConstants.IsWithinSupportedRegion(bbox.MaxLatitude, bbox.MaxLongitude))
            {
                return new ApiResponse<AreaBroadbandInfoDto>
                {
                    Succeeded = false,
                    Message = "Coordinates are outside the supported region.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "BoundingBox", [GeoSpatialConstants.CoordinateRangeErrorMessage] }
                    }
                };
            }

            // Additional validation for reasonable bounding box size (prevent extremely large areas)
            var latDiff = bbox.MaxLatitude - bbox.MinLatitude;
            var lonDiff = bbox.MaxLongitude - bbox.MinLongitude;
            
            if (latDiff > GeoSpatialConstants.MaxLatitudeDifference || lonDiff > GeoSpatialConstants.MaxLongitudeDifference)
            {
                return new ApiResponse<AreaBroadbandInfoDto>
                {
                    Succeeded = false,
                    Message = "Selected area is too large for analysis.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "BoundingBox", [GeoSpatialConstants.GetAreaSizeErrorMessage(latDiff, lonDiff)] }
                    }
                };
            }

            // 2. Create polygon from bounding box
            var envelope = CreatePolygonFromBoundingBox(bbox);

            // 3. Apply envelope filter to queries using the helper
            var cellQuery = _cellRepository.Query(c => !c.IsDeleted);
            cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(envelope, cellQuery);

            var napQuery = _napRepository.Query(n => !n.IsDeleted);
            napQuery = GeoQueryHelper.ApplyEnvelopeFilter(envelope, napQuery);

            // 4. Execute queries and get results with related data
            var cellsWithProvider = await _cellRepository.ToListAsync(
                cellQuery.Select(c => new
                {
                    c.Id,
                    c.ServiceProviderId,
                    ServiceProvider = new
                    {
                        c.ServiceProvider.Id,
                        c.ServiceProvider.Provider.Name,
                        c.ServiceProvider.Provider.Color,
                        c.ServiceProvider.Service.Type
                    }
                }),
                cancellationToken);

            var napsWithProvider = await _napRepository.ToListAsync(
                napQuery.Select(n => new
                {
                    n.Id,
                    n.ServiceProviderId,
                    ServiceProvider = new
                    {
                        n.ServiceProvider.Id,
                        n.ServiceProvider.Provider.Name,
                        n.ServiceProvider.Provider.Color,
                        n.ServiceProvider.Service.Type
                    }
                }),
                cancellationToken);

            // 5. Group by service provider and calculate counts
            var allProviders = cellsWithProvider.Select(c => c.ServiceProvider)
                .Concat(napsWithProvider.Select(n => n.ServiceProvider))
                .GroupBy(sp => sp.Id)
                .Select(g => g.First()) // Take first occurrence of each provider
                .ToList();

            var serviceProviders = allProviders.Select(sp => new ServiceProviderAreaInfoDto
            {
                Id = sp.Id,
                Name = sp.Name,
                Color = sp.Color,
                ServiceType = sp.Type,
                CellCount = cellsWithProvider.Count(c => c.ServiceProvider.Id == sp.Id),
                NAPCount = napsWithProvider.Count(n => n.ServiceProvider.Id == sp.Id)
            }).Where(sp => sp.TotalAssets > 0) // Only include providers with assets in the area
            .OrderByDescending(sp => sp.TotalAssets)
            .ToList();

            // 6. Calculate envelope information
            var envelopeInfo = CalculateEnvelopeInfo(envelope, bbox);

            // 7. Build response
            var result = new AreaBroadbandInfoDto
            {
                CellCount = cellsWithProvider.Count,
                NAPCount = napsWithProvider.Count,
                ServiceProviders = serviceProviders,
                EnvelopeInfo = envelopeInfo
            };

            return new ApiResponse<AreaBroadbandInfoDto>
            {
                Succeeded = true,
                Message = $"Area broadband info retrieved successfully. Found {result.CellCount} cells and {result.NAPCount} NAPs from {serviceProviders.Count} service providers.",
                Data = result
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<AreaBroadbandInfoDto>
            {
                Succeeded = false,
                Message = "An error occurred while processing the area broadband info request.",
                Errors = new Dictionary<string, string[]>
                {
                    { "ProcessingError", [ex.Message] }
                }
            };
        }
    }

    private static Polygon CreatePolygonFromBoundingBox(BoundingBoxDto bbox)
    {
        // Create a polygon from the bounding box coordinates
        var coordinates = new[]
        {
            new Coordinate(bbox.MinLongitude, bbox.MinLatitude), // Bottom-left
            new Coordinate(bbox.MaxLongitude, bbox.MinLatitude), // Bottom-right  
            new Coordinate(bbox.MaxLongitude, bbox.MaxLatitude), // Top-right
            new Coordinate(bbox.MinLongitude, bbox.MaxLatitude), // Top-left
            new Coordinate(bbox.MinLongitude, bbox.MinLatitude)  // Close the ring
        };

        return GeometryHelper.CreatePolygon(coordinates);
    }

    private static AreaEnvelopeInfoDto CalculateEnvelopeInfo(Geometry envelope, BoundingBoxDto bbox)
    {
        // Calculate area in km² using the Haversine approximation for small areas
        var latDiff = bbox.MaxLatitude - bbox.MinLatitude;
        var lonDiff = bbox.MaxLongitude - bbox.MinLongitude;
        var avgLat = (bbox.MinLatitude + bbox.MaxLatitude) / 2;
        
        // Convert degrees to km using geospatial constants
        var latKm = latDiff * GeoSpatialConstants.KilometersPerDegreeLatitude;
        var lonKm = lonDiff * GeoSpatialConstants.GetKilometersPerDegreeLongitude(avgLat);
        var areaKm2 = Math.Abs(latKm * lonKm);

        return new AreaEnvelopeInfoDto
        {
            AreaSizeKm2 = Math.Round(areaKm2, 2),
            GeometryType = envelope.GeometryType,
            BoundingBox = bbox
        };
    }
}
