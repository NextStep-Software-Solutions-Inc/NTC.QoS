using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using System.Diagnostics;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetAllServiceProvidersHandler(
    IServiceProviderRepository serviceProviderRepository,
    IRepository<Domain.Entities.Region> regionRepository,
    ILogger<GetAllServiceProvidersHandler> logger) : IRequestHandler<GetAllServiceProvidersRequest, PaginatedResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;
    private readonly ILogger<GetAllServiceProvidersHandler> _logger = logger;

    // Define the regions that belong to each major area
    private static readonly Dictionary<string, string[]> AreaRegions = new()
    {
        ["luzon"] =
        [
            "Region I (Ilocos Region)", "Region II (Cagayan Valley)", "Region III (Central Luzon)",
            "Region IV-A (Calabarzon)", "Mimaropa Region", "Region V (Bicol Region)",
            "National Capital Region (NCR)", "Cordillera Administrative Region (CAR)"
        ],
        ["visayas"] =
        [
            "Region VI (Western Visayas)", "Region VII (Central Visayas)", "Region VIII (Eastern Visayas)"
        ],
        ["mindanao"] =
        [
            "Region IX (Zamboanga Peninsula)", "Region X (Northern Mindanao)", "Region XI (Davao Region)",
            "Region XII (Soccsksargen)", "Region XIII (Caraga)",
            "Bangsamoro Autonomous Region In Muslim Mindanao (BARMM)"
        ]
    };

    public async Task<PaginatedResponse<ServiceProviderDto>> Handle(GetAllServiceProvidersRequest request, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(3)); // 3-minute timeout

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting GetAllServiceProviders query with filters: Areas={Areas}, Regions={Regions}",
            string.Join(",", request.Areas ?? []), string.Join(",", request.Regions ?? []));

        try
        {
            var allServiceProvidersQuery = _serviceProviderRepository.Query();

            // Early performance optimization: Apply non-spatial filters first to reduce dataset
            allServiceProvidersQuery = ApplyBasicFilters(allServiceProvidersQuery, request);

            var filterStopwatch = Stopwatch.StartNew();

            // Optimized geographic filtering with better performance strategy
            if ((request.Areas?.Count > 0) || (request.Regions?.Count > 0))
            {
                allServiceProvidersQuery = await ApplyGeographicFiltersAsync(allServiceProvidersQuery, request, timeoutCts.Token);
            }

            filterStopwatch.Stop();
            _logger.LogInformation("Geographic filtering completed in {FilterTime}ms", filterStopwatch.ElapsedMilliseconds);

            var serviceProviderDtosQuery = allServiceProvidersQuery
                .OrderBy(s => s.CreatedAt)
                .Select(serviceProvider => new ServiceProviderDto
                {
                    Id = serviceProvider.Id,
                    Provider = new()
                    {
                        Id = serviceProvider.ProviderId,
                        Name = serviceProvider.Provider.Name,
                        Description = serviceProvider.Provider.Description,
                        LicenseNumber = serviceProvider.Provider.LicenseNumber,
                        Color = serviceProvider.Provider.Color
                    },
                    Service = new()
                    {
                        Id = serviceProvider.ServiceId,
                        Name = serviceProvider.Service.Name,
                        Type = serviceProvider.Service.Type.ToString()
                    },
                    MNC = serviceProvider.MNC,
                    MCC = serviceProvider.MCC
                });

            var result = await _serviceProviderRepository.PaginateAsync(serviceProviderDtosQuery, request.PageNumber, request.PageSize, timeoutCts.Token);

            stopwatch.Stop();
            _logger.LogInformation("Query completed successfully in {TotalTime}ms, returned {Count} items",
                stopwatch.ElapsedMilliseconds, result.Data?.Count ?? 0);

            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogError("Query timed out after {ElapsedTime}ms", stopwatch.ElapsedMilliseconds);
            throw new TimeoutException($"Query timed out after {stopwatch.ElapsedMilliseconds}ms. Please try with more specific filters.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error occurred during query execution after {ElapsedTime}ms with Areas={Areas}, Regions={Regions}",
                stopwatch.ElapsedMilliseconds,
                string.Join(",", request.Areas ?? []),
                string.Join(",", request.Regions ?? []));
            throw;
        }
    }

    /// <summary>
    /// Apply non-spatial filters first to reduce the dataset size
    /// </summary>
    private IQueryable<Domain.Aggregates.ServiceProvider> ApplyBasicFilters(
        IQueryable<Domain.Aggregates.ServiceProvider> query,
        GetAllServiceProvidersRequest request)
    {
        // Provider ID filter
        if (request.ProviderIds?.Count > 0)
        {
            query = query.Where(s => request.ProviderIds.Contains(s.ProviderId));
        }

        // Service ID filter
        if (request.ServiceIds?.Count > 0)
        {
            query = query.Where(s => request.ServiceIds.Contains(s.ServiceId));
        }

        // Service type filter - apply early for better performance
        if (request.ServiceTypes?.Count > 0)
        {
            var requestedServiceTypes = request.ServiceTypes
                .Select(st => EnumHelper<ServiceTypeEnum>.GetValue(st))
                .ToHashSet();

            query = query.Where(s => requestedServiceTypes.Contains(s.Service.Type));
        }

        return query;
    }

    /// <summary>
    /// Apply geographic filters using an optimized approach with better error handling
    /// </summary>
    private async Task<IQueryable<Domain.Aggregates.ServiceProvider>> ApplyGeographicFiltersAsync(
        IQueryable<Domain.Aggregates.ServiceProvider> query,
        GetAllServiceProvidersRequest request,
        CancellationToken cancellationToken)
    {
        var targetRegions = GetTargetRegions(request.Areas, request.Regions);

        if (targetRegions.Count == 0)
            return query;

        _logger.LogInformation("Applying geographic filter for {RegionCount} regions: {Regions}",
            targetRegions.Count, string.Join(", ", targetRegions));

        try
        {
            // Always use the individual region approach for better reliability
            return await ApplyIndividualRegionFilterAsync(query, targetRegions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Geographic filtering failed for regions: {Regions}. Falling back to bounding box approach.",
                string.Join(", ", targetRegions));

            // Fallback to bounding box approach
            return await ApplyBoundingBoxFallbackAsync(query, targetRegions, cancellationToken);
        }
    }

    /// <summary>
    /// Apply filtering using individual regions - EF Core translatable approach
    /// </summary>
    private async Task<IQueryable<Domain.Aggregates.ServiceProvider>> ApplyIndividualRegionFilterAsync(
        IQueryable<Domain.Aggregates.ServiceProvider> query,
        HashSet<string> targetRegions,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get region names that exist in database
            var existingRegionNames = await _regionRepository
                .Query()
                .Where(r => targetRegions.Contains(r.Name) && r.Boundaries != null)
                .Select(r => r.Name)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} regions out of {Requested} requested regions: {Regions}",
                existingRegionNames.Count, targetRegions.Count, string.Join(", ", existingRegionNames));

            if (existingRegionNames.Count == 0)
            {
                _logger.LogWarning("No region boundaries found for requested regions: {Regions}",
                    string.Join(", ", targetRegions));
                return query.Where(sp => false); // No matching regions found
            }

            // Log missing regions
            var missingRegions = targetRegions.Except(existingRegionNames).ToList();
            if (missingRegions.Count > 0)
            {
                _logger.LogWarning("Missing region boundaries for: {MissingRegions}", string.Join(", ", missingRegions));
            }

            // Since EF Core has issues with complex spatial queries, go straight to client-side evaluation
            _logger.LogInformation("Using client-side evaluation for spatial filtering due to EF Core limitations");
            return await ApplyClientSideFilteringAsync(query, targetRegions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Individual region filtering failed, trying client-side evaluation");
            return await ApplyClientSideFilteringAsync(query, targetRegions, cancellationToken);
        }
    }

    /// <summary>
    /// Client-side evaluation approach when EF Core translation fails
    /// </summary>
    private async Task<IQueryable<Domain.Aggregates.ServiceProvider>> ApplyClientSideFilteringAsync(
        IQueryable<Domain.Aggregates.ServiceProvider> query,
        HashSet<string> targetRegions,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Using client-side evaluation approach for spatial filtering");

            // Get region boundaries in memory
            var regionBoundaries = await _regionRepository
                .Query(r => targetRegions.Contains(r.Name) && r.Boundaries != null)
                .Select(r => new { r.Name, r.Boundaries })
                .ToListAsync(cancellationToken);

            if (regionBoundaries.Count == 0)
            {
                return query.Where(sp => false);
            }

            // Get all service providers with their locations and evaluate in memory
            var serviceProvidersWithLocations = await query
                .Select(sp => new {
                    ServiceProvider = sp,
                    ServiceType = sp.Service.Type,
                    CellLocations = sp.Cells.Where(c => c.Location != null).Select(c => c.Location),
                    NAPLocations = sp.NAPs.Where(n => n.Location != null).Select(n => n.Location)
                })
                .ToListAsync(cancellationToken);

            // Filter in memory
            var matchingIds = new List<Guid>();
            foreach (var item in serviceProvidersWithLocations)
            {
                bool hasMatch = false;
                IEnumerable<Point?> locations;

                // Determine which locations to check based on service type
                if (item.ServiceType == ServiceTypeEnum.Wireless)
                {
                    locations = item.CellLocations;
                }
                else if (item.ServiceType == ServiceTypeEnum.Wired)
                {
                    locations = item.NAPLocations;
                }
                else
                {
                    continue; // Skip unknown service types
                }

                foreach (var location in locations)
                {
                    if (location == null) continue;

                    foreach (var regionBoundary in regionBoundaries)
                    {
                        try
                        {
                            if (regionBoundary.Boundaries!.Intersects(location))
                            {
                                hasMatch = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Spatial intersection failed for region {Region}", regionBoundary.Name);
                            continue;
                        }
                    }
                    if (hasMatch) break;
                }

                if (hasMatch)
                {
                    matchingIds.Add(item.ServiceProvider.Id);
                }
            }

            _logger.LogInformation("Client-side evaluation found {Count} matching service providers", matchingIds.Count);

            // Return filtered query based on matching IDs
            return query.Where(sp => matchingIds.Contains(sp.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client-side filtering failed");
            throw;
        }
    }

    /// <summary>
    /// Remove the unused database-side filtering method
    /// </summary>
    private Task<IQueryable<Domain.Aggregates.ServiceProvider>> ApplyDatabaseSideFilteringAsync(
        IQueryable<Domain.Aggregates.ServiceProvider> query,
        HashSet<string> targetRegions,
        CancellationToken cancellationToken)
    {
        // This method is no longer needed, redirect to client-side
        return ApplyClientSideFilteringAsync(query, targetRegions, cancellationToken);
    }

    /// <summary>
    /// Fallback approach using bounding boxes when geometric operations fail
    /// </summary>
    private async Task<IQueryable<Domain.Aggregates.ServiceProvider>> ApplyBoundingBoxFallbackAsync(
        IQueryable<Domain.Aggregates.ServiceProvider> query,
        HashSet<string> targetRegions,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Using bounding box fallback approach for {RegionCount} regions", targetRegions.Count);

            // Get envelopes (bounding boxes) instead of complex boundaries
            var regionEnvelopes = await _regionRepository
                .Query(r => targetRegions.Contains(r.Name) && r.Boundaries != null)
                .Select(r => new {
                    r.Name,
                    Envelope = r.Boundaries!.EnvelopeInternal,
                    SRID = r.Boundaries.SRID
                })
                .ToListAsync(cancellationToken);

            if (regionEnvelopes.Count == 0)
            {
                _logger.LogError("No region envelopes found for bounding box fallback");
                return query.Where(sp => false);
            }

            // Create individual bounding box geometries
            var factory = NtsGeometryServices.Instance.CreateGeometryFactory(regionEnvelopes.First().SRID);
            var boundingBoxes = new List<Geometry>();

            foreach (var envelope in regionEnvelopes)
            {
                try
                {
                    var bbox = factory.ToGeometry(envelope.Envelope);
                    boundingBoxes.Add(bbox);
                    _logger.LogDebug("Created bounding box for region: {RegionName}", envelope.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create bounding box for region: {RegionName}", envelope.Name);
                }
            }

            if (boundingBoxes.Count == 0)
            {
                _logger.LogError("No valid bounding boxes created");
                return query.Where(sp => false);
            }

            // Apply bounding box filter
            return query.Where(sp =>
                boundingBoxes.Any(bbox =>
                    (sp.Service.Type == ServiceTypeEnum.Wireless &&
                     sp.Cells.Any(c => c.Location != null && bbox.Intersects(c.Location))) ||
                    (sp.Service.Type == ServiceTypeEnum.Wired &&
                     sp.NAPs.Any(n => n.Location != null && bbox.Intersects(n.Location)))
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bounding box fallback also failed");
            throw new InvalidOperationException($"All geographic filtering approaches failed for regions: {string.Join(", ", targetRegions)}", ex);
        }
    }

    /// <summary>
    /// Efficiently collects target regions from both Areas and direct Regions parameters
    /// </summary>
    private static HashSet<string> GetTargetRegions(List<string>? areas, List<string>? regions)
    {
        var targetRegions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add regions from Areas mapping
        if (areas?.Count > 0)
        {
            foreach (var area in areas)
            {
                var areaKey = area.Trim().ToLowerInvariant(); // Use ToLowerInvariant for better consistency
                if (AreaRegions.TryGetValue(areaKey, out var mappedRegions))
                {
                    foreach (var region in mappedRegions)
                    {
                        targetRegions.Add(region);
                    }
                }
            }
        }

        // Add direct regions
        if (regions?.Count > 0)
        {
            foreach (var region in regions)
            {
                targetRegions.Add(region.Trim());
            }
        }

        return targetRegions;
    }
}