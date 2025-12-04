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

public class GetPointBroadbandInfoHandler : IRequestHandler<GetPointBroadbandInfoRequest, ApiResponse<AreaBroadbandInfoDto>>
{
    private readonly ICellRepository _cellRepository;
    private readonly INAPRepository _napRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly ICellularCoverageRepository _cellularCoverageRepository;
    private readonly ICellularRepository _cellularRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly ILogger<GetPointBroadbandInfoHandler> _logger;

    // Cache for PropertyInfo to avoid repeated reflection overhead
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> LocationPropertyCache = new();

    public GetPointBroadbandInfoHandler(
        ICellRepository cellRepository,
        INAPRepository napRepository,
        IMunicipalityRepository municipalityRepository,
        ICellularCoverageRepository cellularCoverageRepository,
        ICellularRepository cellularRepository,
        ISpeedtestRepository speedtestRepository,
        ILogger<GetPointBroadbandInfoHandler> logger)
    {
        _cellRepository = cellRepository;
        _napRepository = napRepository;
        _municipalityRepository = municipalityRepository;
        _cellularCoverageRepository = cellularCoverageRepository;
        _cellularRepository = cellularRepository;
        _speedtestRepository = speedtestRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<AreaBroadbandInfoDto>> Handle(GetPointBroadbandInfoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pointRadius = request.PointRadius;
            _logger.LogInformation("Processing point broadband info for location: {Lat}, {Lon} with radius: {Radius}m",
                pointRadius.Latitude, pointRadius.Longitude, pointRadius.RadiusMeters);

            // Create a circular buffer around the point
            var centerPoint = GeometryHelper.CreatePoint(new Coordinate(pointRadius.Longitude, pointRadius.Latitude));
            var circularArea = CreateCircularBuffer(centerPoint, pointRadius.RadiusMeters);

            // Apply spatial filter to queries
            var cellQuery = _cellRepository.Query();
            cellQuery = GeoQueryHelper.ApplyEnvelopeFilter(circularArea, cellQuery);

            var napQuery = _napRepository.Query();
            napQuery = GeoQueryHelper.ApplyEnvelopeFilter(circularArea, napQuery);
            
            // Query for cellular coverage (signal strength data)
            var cellularCoverageQuery = _cellularCoverageRepository.Query();
            cellularCoverageQuery = GeoQueryHelper.ApplyEnvelopeFilter(circularArea, cellularCoverageQuery);
            
            // Query for cellular bandwidth data
            var cellularQuery = _cellularRepository.Query();
            
            // Query for speedtest data
            var speedtestQuery = _speedtestRepository.Query();
            speedtestQuery = GeoQueryHelper.ApplyEnvelopeFilter(circularArea, speedtestQuery);

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

            _logger.LogInformation("Found {CellCount} cells and {NAPCount} NAPs within {Radius}m of the specified point",
                cellsWithProvider.Count, napsWithProvider.Count, pointRadius.RadiusMeters);

            // Calculate distances and add proximity information (using direct property access)
            var cellsWithDistance = cellsWithProvider.Select(c => new
            {
                c.Id,
                c.ServiceProviderId,
                c.Location,
                c.ServiceProvider,
                DistanceMeters = CalculateDistance(centerPoint, c.Location)
            }).ToList();

            var napsWithDistance = napsWithProvider.Select(n => new
            {
                n.Id,
                n.ServiceProviderId,
                n.Location,
                n.ServiceProvider,
                DistanceMeters = CalculateDistance(centerPoint, n.Location)
            }).ToList();

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

            _logger.LogInformation("Found {ProviderCount} service providers with assets within the radius", serviceProviders.Count);
            
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

            var municipalities = await CalculateMunicipalityInfo(circularArea, cellsWithProvider, napsWithProvider, cancellationToken);
            _logger.LogInformation("Found {MunicipalityCount} municipalities with assets within the radius", municipalities.Count);
            
            // Get location details from the primary municipality (one with most assets or first covered)
            var locationDetails = GetLocationDetails(municipalities);

            // Calculate area of the circular buffer
            var circularAreaKm2 = Math.PI * Math.Pow(pointRadius.RadiusMeters / 1000.0, 2);

            // Create envelope info for the circular area
            var envelopeInfo = new AreaEnvelopeInfoDto
            {
                AreaSizeKm2 = Math.Round(circularAreaKm2, 2),
                GeometryType = "Circle",
                BoundingBox = CreateBoundingBoxFromCircle(centerPoint, pointRadius.RadiusMeters),
                CenterPoint = new PointDto
                {
                    Latitude = pointRadius.Latitude,
                    Longitude = pointRadius.Longitude
                },
                RadiusMeters = pointRadius.RadiusMeters
            };

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

            _logger.LogInformation("Successfully processed point broadband info. Summary: {TotalCells} cells, {TotalNaps} NAPs, {TotalAssets} total assets across {ProviderCount} providers and {MunicipalityCount} municipalities",
                summary.TotalCells, summary.TotalNaps, summary.TotalAssets, summary.ServiceProviderCount, summary.MunicipalityCount);

            return new ApiResponse<AreaBroadbandInfoDto>
            {
                Succeeded = true,
                Message = $"Point broadband info retrieved successfully. Found {result.Summary.TotalCells} cells and {result.Summary.TotalNaps} NAPs from {serviceProviders.Count} service providers within {pointRadius.RadiusMeters}m radius.",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing point broadband info request");
            return new ApiResponse<AreaBroadbandInfoDto>
            {
                Succeeded = false,
                Message = "An error occurred while processing the point broadband info request.",
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

        // Find all municipalities covered by the circular query area (regardless of assets)
        var coveredMunicipalities = await GeoQueryHelper.FindMunicipalitiesWithinGeometryAsync(
            queryGeometry, _municipalityRepository, cancellationToken);

        _logger.LogInformation("Found {CoveredCount} municipalities within the circular query area", coveredMunicipalities.Count);

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

    private static Geometry CreateCircularBuffer(Point centerPoint, double radiusMeters)
    {
        // Ensure the center point has the correct SRID before buffer operation
        centerPoint.SRID = 4326;

        // Create a circular buffer around the point
        // NetTopologySuite buffer operation creates a polygon approximating a circle
        // The buffer will preserve the SRID from the input geometry
        var bufferedGeometry = centerPoint.Buffer(ConvertMetersToDegreesApprox(radiusMeters, centerPoint.Y));

        return bufferedGeometry;
    }

    private static double ConvertMetersToDegreesApprox(double meters, double latitude)
    {
        // Approximate conversion from meters to degrees
        // This is an approximation; for more accuracy, you could use a more sophisticated conversion
        var latRadians = latitude * GeoSpatialConstants.DegreesToRadians;
        var metersPerDegreeLongitude = GeoSpatialConstants.KilometersPerDegreeLatitude * 1000 * Math.Cos(latRadians);
        return meters / metersPerDegreeLongitude;
    }

    /// <summary>
    /// Calculates Haversine distance between two points in meters.
    /// Parameters ordered consistently: centerPoint (first parameter) then targetPoint (second parameter).
    /// </summary>
    /// <param name="centerPoint">The center/reference point (first parameter)</param>
    /// <param name="targetPoint">The target point to calculate distance to (second parameter)</param>
    /// <returns>Distance in meters, or double.MaxValue if either point is invalid</returns>
    private static double CalculateDistance(Point centerPoint, Point? targetPoint)
    {
        // Check parameters in consistent order: first parameter first, then second parameter
        if (centerPoint?.IsValid != true || targetPoint?.IsValid != true)
            return double.MaxValue;

        try
        {
            // Calculate Haversine distance in meters
            var lat1Rad = centerPoint.Y * GeoSpatialConstants.DegreesToRadians;
            var lon1Rad = centerPoint.X * GeoSpatialConstants.DegreesToRadians;
            var lat2Rad = targetPoint.Y * GeoSpatialConstants.DegreesToRadians;
            var lon2Rad = targetPoint.X * GeoSpatialConstants.DegreesToRadians;

            var deltaLat = lat2Rad - lat1Rad;
            var deltaLon = lon2Rad - lon1Rad;

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var earthRadiusMeters = 6371000; // Earth's radius in meters

            return earthRadiusMeters * c;
        }
        catch
        {
            return double.MaxValue;
        }
    }

    private static BoundingBoxDto CreateBoundingBoxFromCircle(Point centerPoint, double radiusMeters)
    {
        // Calculate the bounding box that encompasses the circular area
        var degreesRadius = ConvertMetersToDegreesApprox(radiusMeters, centerPoint.Y);

        return new BoundingBoxDto
        {
            MinLatitude = centerPoint.Y - degreesRadius,
            MaxLatitude = centerPoint.Y + degreesRadius,
            MinLongitude = centerPoint.X - degreesRadius,
            MaxLongitude = centerPoint.X + degreesRadius
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