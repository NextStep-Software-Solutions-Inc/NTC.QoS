using MediatR;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Helper;
using System.Collections.Concurrent;
using System.Reflection;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

public class GetAreaBroadbandInfoHandler : IRequestHandler<GetAreaBroadbandInfoRequest, ApiResponse<AreaBroadbandInfoDto>>
{
    private readonly ICellRepository _cellRepository;
    private readonly INAPRepository _napRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly ICellularCoverageRepository _cellularCoverageRepository;
    private readonly ICellularRepository _cellularRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly ILogger<GetAreaBroadbandInfoHandler> _logger;

    // Cache for PropertyInfo to avoid repeated reflection overhead
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> LocationPropertyCache = new();

    public GetAreaBroadbandInfoHandler(
        ICellRepository cellRepository,
        INAPRepository napRepository,
        IMunicipalityRepository municipalityRepository,
        ICellularCoverageRepository cellularCoverageRepository,
        ICellularRepository cellularRepository,
        ISpeedtestRepository speedtestRepository,
        ILogger<GetAreaBroadbandInfoHandler> logger)
    {
        _cellRepository = cellRepository;
        _napRepository = napRepository;
        _municipalityRepository = municipalityRepository;
        _cellularCoverageRepository = cellularCoverageRepository;
        _cellularRepository = cellularRepository;
        _speedtestRepository = speedtestRepository;
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
            
            // Query for cellular coverage (signal strength data)
            var cellularCoverageQuery = _cellularCoverageRepository.Query();
            cellularCoverageQuery = GeoQueryHelper.ApplyEnvelopeFilter(envelope, cellularCoverageQuery);
            
            // Query for cellular bandwidth data
            var cellularQuery = _cellularRepository.Query();
            
            // Query for speedtest data
            var speedtestQuery = _speedtestRepository.Query();
            speedtestQuery = GeoQueryHelper.ApplyEnvelopeFilter(envelope, speedtestQuery);

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
                
            // Get cellular coverage data for signal strength
            var cellularCoverageData = await _cellularCoverageRepository.ToListAsync(
                cellularCoverageQuery.Select(cc => new
                {
                    cc.Location,
                    cc.Strength
                }),
                cancellationToken);
                
            // Get cellular bandwidth data - need to join with cells in the area
            var cellIds = cellsWithProvider.Select(c => c.Id).ToList();
            var cellularBandwidthData = await _cellularRepository.ToListAsync(
                cellularQuery
                    .Where(c => c.CellSector != null && cellIds.Contains(c.CellSector.CellId))
                    .Select(c => new
                    {
                        c.ServiceProviderId,
                        c.Bandwidth,
                        ServiceType = c.ServiceProvider != null && c.ServiceProvider.Service != null 
                            ? c.ServiceProvider.Service.Type 
                            : ServiceTypeEnum.Unknown
                    }),
                cancellationToken);
                
            // Get speedtest data for actual measured speeds
            var speedtestData = await _speedtestRepository.ToListAsync(
                speedtestQuery.Select(s => new
                {
                    s.ServiceProviderId,
                    s.DownloadSpeed,
                    s.UploadSpeed,
                    ServiceType = s.ServiceProvider != null && s.ServiceProvider.Service != null 
                        ? s.ServiceProvider.Service.Type 
                        : ServiceTypeEnum.Unknown
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
                NAPCount = napsWithProvider.Count(n => n.ServiceProvider.Id == sp.Id),
                MaxSpeedMbps = CalculateMaxSpeedForProvider(sp.Id, sp.Type, cellularBandwidthData, speedtestData)
            }).Where(sp => sp.TotalAssets > 0) // Only include providers with assets in the area
            .OrderByDescending(sp => sp.TotalAssets)
            .ToList();

            _logger.LogInformation("Found {ProviderCount} service providers with assets in the area", serviceProviders.Count);
            
            // Calculate signal strength metrics
            var signalStrength = CalculateSignalStrength(cellularCoverageData);
            
            // Calculate overall max speed
            var maxSpeed = serviceProviders.Any(sp => sp.MaxSpeedMbps.HasValue) 
                ? serviceProviders.Max(sp => sp.MaxSpeedMbps ?? 0) 
                : 0;
                
            // Determine service availability
            var serviceAvailability = new ServiceAvailabilityDto
            {
                HasCellular = serviceProviders.Any(sp => sp.ServiceType == ServiceTypeEnum.Wireless),
                HasFiber = serviceProviders.Any(sp => sp.ServiceType == ServiceTypeEnum.Wired),
                HasCable = serviceProviders.Any(sp => sp.ServiceType == ServiceTypeEnum.Wired),
                HasDsl = serviceProviders.Any(sp => sp.ServiceType == ServiceTypeEnum.Wired)
            };

            var municipalities = await CalculateMunicipalityInfo(envelope, cellsWithProvider, napsWithProvider, cancellationToken);
            _logger.LogInformation("Found {MunicipalityCount} municipalities with assets in the area", municipalities.Count);
            
            // Get location details from the primary municipality (one with most assets or first covered)
            var locationDetails = GetLocationDetails(municipalities);

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
                EnvelopeInfo = envelopeInfo,
                LocationDetails = locationDetails,
                InternetServices = new InternetServicesDto
                {
                    ProviderCount = serviceProviders.Count,
                    MaxSpeedMbps = maxSpeed
                },
                SignalStrength = signalStrength,
                ServiceAvailability = serviceAvailability
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

    /// <summary>
    /// Calculates municipality information for items that have location data.
    /// Uses both interface constraints for type safety and fallback reflection for anonymous types.
    /// </summary>
    /// <typeparam name="T1">Type for cells (anonymous type or IHasLocation implementer)</typeparam>
    /// <typeparam name="T2">Type for NAPs (anonymous type or IHasLocation implementer)</typeparam>
    /// <param name="queryGeometry">The geometry to search within</param>
    /// <param name="cellsWithProvider">List of cells with provider information</param>
    /// <param name="napsWithProvider">List of NAPs with provider information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of municipality area information</returns>
    private async Task<List<MunicipalityAreaInfoDto>> CalculateMunicipalityInfo<T1, T2>(
        Geometry queryGeometry,
        List<T1> cellsWithProvider,
        List<T2> napsWithProvider,
        CancellationToken cancellationToken)
        where T1 : class
        where T2 : class
    {
        var cellLocations = cellsWithProvider
                        .Select(c => GetLocationFromItem(c))
                        .Where(loc => loc is not null && loc.IsValid)
                        .Cast<Point>()
                        .ToList();

        var napLocations = napsWithProvider
            .Select(n => GetLocationFromItem(n))
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
            var location = GetLocationFromItem(cell);
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
            var location = GetLocationFromItem(nap);
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

    /// <summary>
    /// Type-safe method to extract Location property from objects.
    /// Handles both interface implementations and anonymous types with optimized caching.
    /// </summary>
    /// <typeparam name="T">Type containing a Location property</typeparam>
    /// <param name="item">Object containing the Location property</param>
    /// <returns>The Point location or null if not available or invalid</returns>
    private static Point? GetLocationFromItem<T>(T item) where T : class
    {
        if (item == null) return null;

        // First try interface-based approach for better type safety
        if (item is IHasLocation hasLocation)
        {
            return hasLocation.Location;
        }

        // Fallback to cached reflection for anonymous types
        try
        {
            var locationType = typeof(T);
            var locationProperty = LocationPropertyCache.GetOrAdd(locationType, type =>
            {
                var property = type.GetProperty("Location");
                return property?.PropertyType == typeof(Point) ? property : null;
            });

            return locationProperty?.GetValue(item) as Point;
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
    
    /// <summary>
    /// Calculates maximum speed for a service provider based on cellular bandwidth data, speedtest data, and service type.
    /// Prioritizes actual speedtest measurements over theoretical bandwidth.
    /// </summary>
    private static decimal? CalculateMaxSpeedForProvider<T1, T2>(Guid providerId, ServiceTypeEnum serviceType, List<T1> cellularBandwidthData, List<T2> speedtestData) 
        where T1 : class 
        where T2 : class
    {
        try
        {
            decimal? maxSpeed = null;
            
            // First, check for actual speedtest measurements for this provider
            if (speedtestData.Any())
            {
                var providerIdProperty = typeof(T2).GetProperty("ServiceProviderId");
                var downloadSpeedProperty = typeof(T2).GetProperty("DownloadSpeed");
                var serviceTypeProperty = typeof(T2).GetProperty("ServiceType");
                
                if (providerIdProperty != null && downloadSpeedProperty != null)
                {
                    var providerSpeeds = speedtestData
                        .Where(s => 
                        {
                            var id = providerIdProperty.GetValue(s);
                            var svcType = serviceTypeProperty?.GetValue(s);
                            return id is Guid guid && guid == providerId && 
                                   (svcType is ServiceTypeEnum type && type == serviceType || serviceType == ServiceTypeEnum.Wired);
                        })
                        .Select(s => downloadSpeedProperty.GetValue(s))
                        .OfType<decimal>()
                        .ToList();
                        
                    if (providerSpeeds.Any())
                    {
                        maxSpeed = providerSpeeds.Max();
                    }
                }
            }
            
            // For wireless/cellular, also check bandwidth data and use the higher value
            if (serviceType == ServiceTypeEnum.Wireless && cellularBandwidthData.Any())
            {
                var providerIdProperty = typeof(T1).GetProperty("ServiceProviderId");
                var bandwidthProperty = typeof(T1).GetProperty("Bandwidth");
                var serviceTypeProperty = typeof(T1).GetProperty("ServiceType");
                
                if (providerIdProperty != null && bandwidthProperty != null)
                {
                    var providerBandwidths = cellularBandwidthData
                        .Where(c => 
                        {
                            var id = providerIdProperty.GetValue(c);
                            var svcType = serviceTypeProperty?.GetValue(c);
                            return id is Guid guid && guid == providerId && 
                                   svcType is ServiceTypeEnum type && type == ServiceTypeEnum.Wireless;
                        })
                        .Select(c => bandwidthProperty.GetValue(c))
                        .OfType<decimal>()
                        .ToList();
                        
                    if (providerBandwidths.Any())
                    {
                        var maxBandwidth = providerBandwidths.Max();
                        maxSpeed = maxSpeed.HasValue ? Math.Max(maxSpeed.Value, maxBandwidth) : maxBandwidth;
                    }
                }
            }
            
            // For wired services without speedtest data, use typical maximum speeds
            if (!maxSpeed.HasValue && serviceType == ServiceTypeEnum.Wired)
            {
                // Default to fiber speeds for wired services
                maxSpeed = 1000m; // 1 Gbps typical for fiber/wired services
            }
            
            return maxSpeed;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Calculates signal strength metrics from cellular coverage data.
    /// </summary>
    private static SignalStrengthDto? CalculateSignalStrength<T>(List<T> cellularCoverageData) where T : class
    {
        if (!cellularCoverageData.Any())
        {
            return null;
        }
        
        try
        {
            var strengthProperty = typeof(T).GetProperty("Strength");
            if (strengthProperty == null)
            {
                return null;
            }
            
            var strengths = cellularCoverageData
                .Select(c => strengthProperty.GetValue(c))
                .OfType<decimal>()
                .ToList();
                
            if (!strengths.Any())
            {
                return null;
            }
            
            var avgStrength = strengths.Average();
            var maxStrength = strengths.Max();
            var minStrength = strengths.Min();
            
            // Determine quality based on average strength (in dBm)
            // Signal strength is typically negative, so higher (less negative) is better
            var quality = avgStrength switch
            {
                >= -70 => "Excellent",
                >= -85 => "Good",
                >= -100 => "Fair",
                >= -110 => "Weak",
                _ => "Poor"
            };
            
            return new SignalStrengthDto
            {
                AverageStrengthDbm = Math.Round(avgStrength, 2),
                MaxStrengthDbm = maxStrength,
                MinStrengthDbm = minStrength,
                Quality = quality,
                MeasurementCount = strengths.Count
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Extracts location details from municipality information.
    /// </summary>
    private static LocationDetailsDto? GetLocationDetails(List<MunicipalityAreaInfoDto> municipalities)
    {
        if (!municipalities.Any())
        {
            return null;
        }
        
        // Get the primary municipality (one with most assets or first covered)
        var primaryMunicipality = municipalities
            .Where(m => m.HasAssets)
            .OrderByDescending(m => m.TotalAssets)
            .ThenBy(m => m.Name)
            .FirstOrDefault() ?? municipalities.FirstOrDefault();
            
        if (primaryMunicipality == null)
        {
            return null;
        }
        
        // Parse municipality name - typically might include province info
        // This is a basic implementation; you may need to enhance with actual barangay/province queries
        return new LocationDetailsDto
        {
            Municipality = primaryMunicipality.Name,
            // Note: Street, Barangay, Province, and Region would require additional queries
            // or reverse geocoding services to populate accurately
        };
    }
}
