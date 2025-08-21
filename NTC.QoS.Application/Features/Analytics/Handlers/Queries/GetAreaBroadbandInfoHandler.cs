using MediatR;
using Microsoft.Extensions.Logging;
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

            var municipalities = await CalculateMunicipalityInfo(envelope, cellsWithProvider, napsWithProvider, cancellationToken);

            var envelopeInfo = CalculateEnvelopeInfo(envelope, bbox);

            var summary = new AreaSummaryDto
            {
                TotalCells = cellsWithProvider.Count,
                TotalNaps = napsWithProvider.Count,
                TotalAssets = cellsWithProvider.Count + napsWithProvider.Count,
                CoverageAreaKm2 = envelopeInfo.AreaSizeKm2,
                ServiceProviderCount = serviceProviders.Count,
                MunicipalityCount = municipalities.Count, // Now includes all covered municipalities
                MunicipalitiesWithAssets = municipalities.Count(m => m.HasAssets),
                MunicipalitiesWithoutAssets = municipalities.Count(m => m.IsCoveredByQuery && !m.HasAssets),
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

            return new ApiResponse<AreaBroadbandInfoDto>
            {
                Succeeded = true,
                Message = $"Area broadband info retrieved successfully. Found {result.Summary.TotalCells} cells and {result.Summary.TotalNaps} NAPs from {serviceProviders.Count} service providers across {municipalities.Count} municipalities.",
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

    private async Task<List<MunicipalityAreaInfoDto>> CalculateMunicipalityInfo<T1, T2>(
        Geometry queryGeometry,
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

        // Find all municipalities covered by the query area (regardless of assets)
        var coveredMunicipalities = await GeoQueryHelper.FindMunicipalitiesWithinGeometryAsync(
            queryGeometry, _municipalityRepository, cancellationToken);

        _logger.LogInformation("Found {CoveredCount} municipalities within the query area", coveredMunicipalities.Count);

        // Find municipalities for asset locations (if any)
        var locationMunicipalityMap = new Dictionary<Point, (Guid? Id, string Name)>();
        if (allLocations.Count > 0)
        {
            locationMunicipalityMap = await GeoQueryHelper.FindMunicipalitiesByLocationsAsync(
                allLocations, _municipalityRepository, cancellationToken);
        }

        // Group by municipality and count assets
        var municipalityGroups = new Dictionary<(Guid? Id, string Name), (int CellCount, int NAPCount, bool IsCovered)>();

        // Initialize all covered municipalities with zero assets
        foreach (var coveredMunicipality in coveredMunicipalities)
        {
            var key = (coveredMunicipality.Id, coveredMunicipality.Name);
            municipalityGroups[key] = (0, 0, true);
        }

        // Count cells by municipality
        foreach (var cell in cellsWithProvider)
        {
            var location = GetLocationFromDynamic(cell);
            if (location?.IsValid == true && locationMunicipalityMap.TryGetValue(location, out var municipalityInfo))
            {
                var key = (municipalityInfo.Id, municipalityInfo.Name);
                if (municipalityGroups.TryGetValue(key, out var value))
                {
                    municipalityGroups[key] = (value.CellCount + 1, value.NAPCount, value.IsCovered);
                }
                else
                {
                    // Asset found in municipality not covered by query geometry (edge case)
                    municipalityGroups[key] = (1, 0, false);
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
                if (municipalityGroups.TryGetValue(key, out var value))
                {
                    municipalityGroups[key] = (value.CellCount, value.NAPCount + 1, value.IsCovered);
                }
                else
                {
                    // Asset found in municipality not covered by query geometry (edge case)
                    municipalityGroups[key] = (0, 1, false);
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
                IsCoveredByQuery = kvp.Value.IsCovered,
                AssetPercentage = totalAssets > 0 
                    ? Math.Round(((double)(kvp.Value.CellCount + kvp.Value.NAPCount) / totalAssets) * 100, 2)
                    : 0
            })
            .OrderByDescending(m => m.HasAssets)        // Assets first
            .ThenByDescending(m => m.TotalAssets)       // Then by asset count
            .ThenByDescending(m => m.IsCoveredByQuery)  // Then covered ones
            .ThenBy(m => m.Name)                        // Finally by name
            .ToList();

        _logger.LogInformation("Municipality calculation completed. Found {MunicipalityCount} total municipalities ({WithAssets} with assets, {CoveredOnly} covered without assets)", 
            result.Count, 
            result.Count(m => m.HasAssets), 
            result.Count(m => m.IsCoveredByQuery && !m.HasAssets));
        
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
