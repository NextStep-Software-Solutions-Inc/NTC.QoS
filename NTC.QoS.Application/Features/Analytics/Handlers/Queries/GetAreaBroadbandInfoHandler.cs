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

public class GetAreaBroadbandInfoHandler : IRequestHandler<GetAreaBroadbandInfoRequest, ApiResponse<AreaBroadbandInfoDto>>
{
    private readonly ICellRepository _cellRepository;
    private readonly INAPRepository _napRepository;
    private readonly ICellularCoverageRepository _cellularCoverageRepository;
    private readonly ICellularRepository _cellularRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly BroadbandAnalyticsService _analyticsService;
    private readonly ILogger<GetAreaBroadbandInfoHandler> _logger;

    public GetAreaBroadbandInfoHandler(
        ICellRepository cellRepository,
        INAPRepository napRepository,
        ICellularCoverageRepository cellularCoverageRepository,
        ICellularRepository cellularRepository,
        ISpeedtestRepository speedtestRepository,
        BroadbandAnalyticsService analyticsService,
        ILogger<GetAreaBroadbandInfoHandler> logger)
    {
        _cellRepository = cellRepository;
        _napRepository = napRepository;
        _cellularCoverageRepository = cellularCoverageRepository;
        _cellularRepository = cellularRepository;
        _speedtestRepository = speedtestRepository;
        _analyticsService = analyticsService;
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
            
            // Query for cellular bandwidth data - apply spatial filter via CellSector.Cell.Location
            var cellularQuery = _cellularRepository.Query()
                .Where(c => c.CellSector != null && 
                           c.CellSector.Cell != null && 
                           c.CellSector.Cell.Location != null &&
                            c.CellSector.Cell.Location.Intersects(envelope));
            
            // Query for speedtest data
            var speedtestQuery = _speedtestRepository.Query();
            speedtestQuery = GeoQueryHelper.ApplyEnvelopeFilter(envelope, speedtestQuery);

            // Execute queries and get results with related data including location
            var cellsWithProvider = await _cellRepository.ToListAsync(
                cellQuery.Select(c => new ProviderAssetDto
                {
                    Id = c.Id,
                    ServiceProviderId = c.ServiceProviderId,
                    Location = c.Location,
                    ServiceProvider = new ProviderInfoDto
                    {
                        Id = c.ServiceProvider.Id,
                        Name = c.ServiceProvider.Provider.Name,
                        Color = c.ServiceProvider.Provider.Color,
                        Type = c.ServiceProvider.Service.Type
                    }
                }),
                cancellationToken);

            var napsWithProvider = await _napRepository.ToListAsync(
                napQuery.Select(n => new ProviderAssetDto
                {
                    Id = n.Id,
                    ServiceProviderId = n.ServiceProviderId,
                    Location = n.Location,
                    ServiceProvider = new ProviderInfoDto
                    {
                        Id = n.ServiceProvider.Id,
                        Name = n.ServiceProvider.Provider.Name,
                        Color = n.ServiceProvider.Provider.Color,
                        Type = n.ServiceProvider.Service.Type
                    }
                }),
                cancellationToken);
                
            // Get cellular coverage data for signal strength
            var cellularCoverageData = await _cellularCoverageRepository.ToListAsync(
                cellularCoverageQuery.Select(cc => new CellularCoverageProjectionDto
                {
                    Location = cc.Location,
                    Strength = cc.Strength
                }),
                cancellationToken);
                
            // Get cellular bandwidth data - spatial filter already applied
            var cellularBandwidthData = await _cellularRepository.ToListAsync(
                cellularQuery
                    .Select(c => new CellularBandwidthProjectionDto
                    {
                        ServiceProviderId = c.ServiceProviderId,
                        Bandwidth = c.Bandwidth,
                        ServiceType = c.ServiceProvider != null && c.ServiceProvider.Service != null
                            ? c.ServiceProvider.Service.Type
                            : ServiceTypeEnum.Unknown
                    }),
                cancellationToken);

            // Get speedtest data for actual measured speeds
            var speedtestData = await _speedtestRepository.ToListAsync(
                speedtestQuery.Select(s => new SpeedtestProjectionDto
                {
                    ServiceProviderId = s.ServiceProviderId,
                    DownloadSpeed = s.DownloadSpeed,
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
                MaxSpeedMbps = _analyticsService.CalculateMaxSpeedForProvider(sp.Id, sp.Type, cellularBandwidthData, speedtestData)
            }).Where(sp => sp.TotalAssets > 0) // Only include providers with assets in the area
            .OrderByDescending(sp => sp.TotalAssets)
            .ToList();

            _logger.LogInformation("Found {ProviderCount} service providers with assets in the area", serviceProviders.Count);
            
            // Calculate signal strength metrics
            var signalStrength = _analyticsService.CalculateSignalStrength(cellularCoverageData);
            
            // Calculate overall max speed
            var maxSpeed = serviceProviders.Any(sp => sp.MaxSpeedMbps.HasValue) 
                ? serviceProviders.Max(sp => sp.MaxSpeedMbps ?? 0) 
                : 0;
                
            // Determine service availability
            // Note: HasFiber uses ServiceTypeEnum.Wired. Currently, the system only has fiber as the wired service type.
            // If other wired types (cable, DSL) are added in the future, consider renaming to HasWired or filtering specifically for fiber.
            var serviceAvailability = new ServiceAvailabilityDto
            {
                HasCellular = serviceProviders.Any(sp => sp.ServiceType == ServiceTypeEnum.Wireless),
                HasFiber = serviceProviders.Any(sp => sp.ServiceType == ServiceTypeEnum.Wired)
            };

            // Build lists of locations to pass to the analytics service (uses strongly-typed projections)
            var cellLocations = cellsWithProvider
                .Where(c => c.Location != null && c.Location.IsValid)
                .Select(c => c.Location)
                .ToList();

            var napLocations = napsWithProvider
                .Where(n => n.Location != null && n.Location.IsValid)
                .Select(n => n.Location)
                .ToList();

            var municipalities = await _analyticsService.CalculateMunicipalityInfo(envelope, cellLocations, napLocations, cancellationToken);
            _logger.LogInformation("Found {MunicipalityCount} municipalities with assets in the area", municipalities.Count);
            
            // Get location details from the primary municipality (one with most assets or first covered)
            var locationDetails = _analyticsService.GetLocationDetails(municipalities);

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
