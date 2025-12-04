using MediatR;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

public class GetPointBroadbandInfoHandler : IRequestHandler<GetPointBroadbandInfoRequest, ApiResponse<AreaBroadbandInfoDto>>
{
    private readonly ICellRepository _cellRepository;
    private readonly INAPRepository _napRepository;
    private readonly ICellularCoverageRepository _cellularCoverageRepository;
    private readonly ICellularRepository _cellularRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly BroadbandAnalyticsService _analyticsService;
    private readonly ILogger<GetPointBroadbandInfoHandler> _logger;

    public GetPointBroadbandInfoHandler(
        ICellRepository cellRepository,
        INAPRepository napRepository,
        ICellularCoverageRepository cellularCoverageRepository,
        ICellularRepository cellularRepository,
        ISpeedtestRepository speedtestRepository,
        BroadbandAnalyticsService analyticsService,
        ILogger<GetPointBroadbandInfoHandler> logger)
    {
        _cellRepository = cellRepository;
        _napRepository = napRepository;
        _cellularCoverageRepository = cellularCoverageRepository;
        _cellularRepository = cellularRepository;
        _speedtestRepository = speedtestRepository;
        _analyticsService = analyticsService;
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
                MaxSpeedMbps = _analyticsService.CalculateMaxSpeedForProvider(sp.Id, sp.Type, cellularBandwidthData, speedtestData)
            }).Where(sp => sp.TotalAssets > 0) // Only include providers with assets in the area
            .OrderByDescending(sp => sp.TotalAssets)
            .ToList();

            _logger.LogInformation("Found {ProviderCount} service providers with assets within the radius", serviceProviders.Count);
            
            // Calculate signal strength metrics
            var signalStrength = _analyticsService.CalculateSignalStrength(cellularCoverageData);
            
            // Calculate overall max speed
            var maxSpeed = serviceProviders.Any(sp => sp.MaxSpeedMbps.HasValue) 
                ? serviceProviders.Max(sp => sp.MaxSpeedMbps ?? 0) 
                : 0;
                
            // Determine service availability
            var serviceAvailability = new ServiceAvailabilityDto
            {
                HasCellular = serviceProviders.Any(sp => sp.ServiceType == ServiceTypeEnum.Wireless),
                HasFiber = serviceProviders.Any(sp => sp.ServiceType == ServiceTypeEnum.Wired)
            };

            var municipalities = await _analyticsService.CalculateMunicipalityInfo(circularArea, cellsWithProvider, napsWithProvider, cancellationToken);
            _logger.LogInformation("Found {MunicipalityCount} municipalities with assets within the radius", municipalities.Count);
            
            // Get location details from the primary municipality (one with most assets or first covered)
            var locationDetails = _analyticsService.GetLocationDetails(municipalities);

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
}