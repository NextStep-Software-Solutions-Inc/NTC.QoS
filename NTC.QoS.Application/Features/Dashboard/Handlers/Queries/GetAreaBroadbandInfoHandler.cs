using MediatR;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetAreaBroadbandInfoHandler : IRequestHandler<GetAreaBroadbandInfoRequest, ApiResponse<AreaBroadbandInfoDto>>
{
    private readonly IRepository<Domain.Aggregates.Cell> _cellRepository;
    private readonly IRepository<Domain.Aggregates.NAP> _napRepository;

    public GetAreaBroadbandInfoHandler(
        IRepository<Domain.Aggregates.Cell> cellRepository,
        IRepository<Domain.Aggregates.NAP> napRepository)
    {
        _cellRepository = cellRepository;
        _napRepository = napRepository;
    }

    public async Task<ApiResponse<AreaBroadbandInfoDto>> Handle(GetAreaBroadbandInfoRequest request, CancellationToken cancellationToken)
    {
        try
        {

            if (request.AreaEnvelope == null || string.IsNullOrWhiteSpace(request.AreaEnvelope.GeoJsonString))
            {
                return new ApiResponse<AreaBroadbandInfoDto>
                {
                    Succeeded = false,
                    Message = "GeoJSON string is required.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "GeoJsonString", ["GeoJSON string cannot be null or empty."] }
                    }
                };
            }

            // 1. Parse GeoJSON string to Geometry
            Geometry? envelope;
            var geoJsonReader = new GeoJsonReader();

            try
            {
                // Try to parse as FeatureCollection first
                var featureCollection = geoJsonReader.Read<FeatureCollection>(request.AreaEnvelope.GeoJsonString);
                envelope = featureCollection?
                    .FirstOrDefault(f => f.Geometry is Polygon or MultiPolygon)?
                    .Geometry;

                if (envelope == null)
                {
                    // Try to parse as direct Geometry
                    envelope = geoJsonReader.Read<Geometry>(request.AreaEnvelope.GeoJsonString);
                }
            }
            catch (Newtonsoft.Json.JsonSerializationException)
            {
                // Fallback to direct geometry parsing
                envelope = geoJsonReader.Read<Geometry>(request.AreaEnvelope.GeoJsonString);
            }

            if (envelope == null)
            {
                return new ApiResponse<AreaBroadbandInfoDto>
                {
                    Succeeded = false,
                    Message = "Invalid GeoJSON: Could not parse geometry.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "GeoJson", ["Provided GeoJSON could not be parsed into a valid geometry."] }
                    }
                };
            }

            // Ensure we have a valid geometry type
            if (envelope is not Polygon and not MultiPolygon)
            {
                return new ApiResponse<AreaBroadbandInfoDto>
                {
                    Succeeded = false,
                    Message = "Invalid geometry type: Only Polygon and MultiPolygon are supported.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "GeometryType", [$"Received {envelope.GeometryType}, but only Polygon and MultiPolygon are supported."] }
                    }
                };
            }

            // 2. Apply envelope filter to queries using the helper
            var cellQuery = _cellRepository.Query(c => !c.IsDeleted);
            cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(envelope, cellQuery);

            var napQuery = _napRepository.Query(n => !n.IsDeleted);
            napQuery = GeoQueryHelper.ApplyEnvelopeFilter(envelope, napQuery);

            // 3. Execute queries and get results with related data
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

            // 4. Group by service provider and calculate counts
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

            // 5. Calculate envelope information
            var envelopeInfo = CalculateEnvelopeInfo(envelope);

            // 6. Build response
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

    private static AreaEnvelopeInfoDto CalculateEnvelopeInfo(Geometry envelope)
    {
        var boundingBox = envelope.EnvelopeInternal;
        
        return new AreaEnvelopeInfoDto
        {
            AreaSizeKm2 = Math.Round(envelope.Area * 111.32 * 111.32, 2), // Rough conversion from degrees² to km²
            GeometryType = envelope.GeometryType,
            BoundingBox = new BoundingBoxDto
            {
                MinLatitude = boundingBox.MinY,
                MaxLatitude = boundingBox.MaxY,
                MinLongitude = boundingBox.MinX,
                MaxLongitude = boundingBox.MaxX
            }
        };
    }
}