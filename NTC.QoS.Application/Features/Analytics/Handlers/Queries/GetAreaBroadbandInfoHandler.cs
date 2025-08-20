using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Helper;
using Microsoft.Extensions.Logging;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

public class GetAreaBroadbandInfoHandler : IRequestHandler<GetAreaBroadbandInfoRequest, ApiResponse<AreaBroadbandInfoDto>>
{
    private readonly ICellRepository _cellRepository;
    private readonly INAPRepository _napRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly ILogger<GetAreaBroadbandInfoHandler> _logger;

    public GetAreaBroadbandInfoHandler(
        ICellRepository cellRepository,
        INAPRepository napRepository,
        IMunicipalityRepository municipalityRepository,
        ILogger<GetAreaBroadbandInfoHandler> logger)
    {
        _cellRepository = cellRepository;
        _napRepository = napRepository;
        _municipalityRepository = municipalityRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<AreaBroadbandInfoDto>> Handle(GetAreaBroadbandInfoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validation is now handled by FluentValidation pipeline
            var bbox = request.BoundingBox;
            _logger.LogInformation("Processing area broadband info for bounding box: {MinLat}, {MinLon} to {MaxLat}, {MaxLon}", 
                bbox.MinLatitude, bbox.MinLongitude, bbox.MaxLatitude, bbox.MaxLongitude);

            // Create polygon from bounding box
            var envelope = CreatePolygonFromBoundingBox(bbox);

            // Apply envelope filter to queries using the helper
            var cellQuery = _cellRepository.Query();
            cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(envelope, cellQuery);

            var napQuery = _napRepository.Query();
            napQuery = GeoQueryHelper.ApplyEnvelopeFilter(envelope, napQuery);

            // Execute queries and get results with related data including location
            var cellsWithProvider = await _cellRepository.ToListAsync(
                cellQuery.Select(c => new
                {
                    c.Id,
                    c.ServiceProviderId,
                    c.Location,
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
                    n.Location,
                    ServiceProvider = new
                    {
                        n.ServiceProvider.Id,
                        n.ServiceProvider.Provider.Name,
                        n.ServiceProvider.Provider.Color,
                        n.ServiceProvider.Service.Type
                    }
                }),
                cancellationToken);

            _logger.LogInformation("Found {CellCount} cells and {NAPCount} NAPs in the specified area", 
                cellsWithProvider.Count, napsWithProvider.Count);

            // Group by service provider and calculate counts
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

            _logger.LogInformation("Found {ProviderCount} service providers with assets in the area", serviceProviders.Count);

            var municipalities = await CalculateMunicipalityInfo(cellsWithProvider, napsWithProvider, cancellationToken);
            _logger.LogInformation("Found {MunicipalityCount} municipalities with assets in the area", municipalities.Count);

            // Log municipality details for debugging
            foreach (var municipality in municipalities.Take(5)) // Log first 5 municipalities
            {
                _logger.LogInformation("Municipality: {Name} - Cells: {CellCount}, NAPs: {NAPCount}, Total: {TotalAssets}", 
                    municipality.Name, municipality.CellCount, municipality.NAPCount, municipality.TotalAssets);
            }

            var envelopeInfo = CalculateEnvelopeInfo(envelope, bbox);

            var summary = new AreaSummaryDto
            {
                TotalCells = cellsWithProvider.Count,
                TotalNaps = napsWithProvider.Count,
                TotalAssets = cellsWithProvider.Count + napsWithProvider.Count,
                CoverageAreaKm2 = envelopeInfo.AreaSizeKm2,
                ServiceProviderCount = serviceProviders.Count,
                MunicipalityCount = municipalities.Count,
                AssetDensityPerKm2 = envelopeInfo.AreaSizeKm2 > 0
                    ? Math.Round((double)(cellsWithProvider.Count + napsWithProvider.Count) / envelopeInfo.AreaSizeKm2, 6)
                    : 0
            };

            // Build response
            var result = new AreaBroadbandInfoDto
            {
                Summary = summary,
                ServiceProviders = serviceProviders,
                Municipalities = municipalities,
                EnvelopeInfo = envelopeInfo
            };

            _logger.LogInformation("Successfully processed area broadband info. Summary: {TotalCells} cells, {TotalNaps} NAPs, {TotalAssets} total assets across {ProviderCount} providers and {MunicipalityCount} municipalities", 
                summary.TotalCells, summary.TotalNaps, summary.TotalAssets, summary.ServiceProviderCount, summary.MunicipalityCount);

            return new ApiResponse<AreaBroadbandInfoDto>
            {
                Succeeded = true,
                Message = $"Area broadband info retrieved successfully. Found {result.Summary.TotalCells} cells and {result.Summary.TotalNaps} NAPs from {serviceProviders.Count} service providers across {municipalities.Count} municipalities.",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing area broadband info request");
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

    private async Task<List<MunicipalityAreaInfoDto>> CalculateMunicipalityInfo<T1, T2>(
        List<T1> cellsWithProvider, 
        List<T2> napsWithProvider, 
        CancellationToken cancellationToken)
        where T1 : class
        where T2 : class
    {
        var cellLocations = cellsWithProvider
                        .Select(c => GetLocationFromDynamic(c))
                        .Where(loc => loc is not null && loc.IsValid)
                        .Cast<Point>()
                        .ToList();

        var napLocations = napsWithProvider
            .Select(n => GetLocationFromDynamic(n))
            .Where(loc => loc is not null && loc.IsValid)
            .Cast<Point>()
            .ToList();

        var allLocations = cellLocations.Concat(napLocations).ToList();

        if (allLocations.Count == 0)
        {
            _logger.LogWarning("No valid locations found for municipality calculation");
            return new List<MunicipalityAreaInfoDto>();
        }

        // Find municipalities for all locations
        var locationMunicipalityMap = await GeoQueryHelper.FindMunicipalitiesByLocationsAsync(
            allLocations, _municipalityRepository, cancellationToken);

        // Group by municipality and count assets
        var municipalityGroups = new Dictionary<(Guid? Id, string Name), (int CellCount, int NAPCount)>();

        // Count cells by municipality
        foreach (var cell in cellsWithProvider)
        {
            var location = GetLocationFromDynamic(cell);
            if (location?.IsValid == true && locationMunicipalityMap.TryGetValue(location, out var municipalityInfo))
            {
                var key = (municipalityInfo.Id, municipalityInfo.Name);
                if (municipalityGroups.TryGetValue(key, out (int CellCount, int NAPCount) value))
                {
                    municipalityGroups[key] = (value.CellCount + 1, value.NAPCount);
                }
                else
                {
                    municipalityGroups[key] = (1, 0);
                }
            }
        }

        // Count NAPs by municipality
        foreach (var nap in napsWithProvider)
        {
            var location = GetLocationFromDynamic(nap);
            if (location?.IsValid == true && locationMunicipalityMap.TryGetValue(location, out var municipalityInfo))
            {
                var key = (municipalityInfo.Id, municipalityInfo.Name);
                if (municipalityGroups.TryGetValue(key, out (int CellCount, int NAPCount) value))
                {
                    municipalityGroups[key] = (value.CellCount, value.NAPCount + 1);
                }
                else
                {
                    municipalityGroups[key] = (0, 1);
                }
            }
        }

        // Calculate total assets for percentage calculation
        var totalAssets = cellsWithProvider.Count + napsWithProvider.Count;

        var result = municipalityGroups
            .Select(kvp => new MunicipalityAreaInfoDto
            {
                Id = kvp.Key.Id,
                Name = kvp.Key.Name,
                CellCount = kvp.Value.CellCount,
                NAPCount = kvp.Value.NAPCount,
                AssetPercentage = totalAssets > 0 
                    ? Math.Round(((double)(kvp.Value.CellCount + kvp.Value.NAPCount) / totalAssets) * 100, 2)
                    : 0
            })
            .Where(m => m.TotalAssets > 0)
            .OrderByDescending(m => m.TotalAssets)
            .ThenBy(m => m.Name)
            .ToList();

        _logger.LogInformation("Municipality calculation completed. Found {MunicipalityCount} municipalities with assets", result.Count);
        
        return result;
    }

    private static Point? GetLocationFromDynamic(dynamic item)
    {
        try
        {
            return item?.Location as Point;
        }
        catch
        {
            return null;
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
