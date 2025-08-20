using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetAllServiceProvidersHandler(
    IServiceProviderRepository serviceProviderRepository, 
    IRepository<Domain.Entities.Region> regionRepository) : IRequestHandler<GetAllServiceProvidersRequest, PaginatedResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;

    // Define the regions that belong to each major area
    private static readonly Dictionary<string, string[]> AreaRegions = new()
    {
        ["luzon"] =
        [
            "Ilocos Region", "Cagayan Valley", "Central Luzon", "CALABARZON", "MIMAROPA",
            "Bicol Region", "National Capital Region", "Cordillera Administrative Region"
        ],
        ["visayas"] =
        [
            "Western Visayas", "Central Visayas", "Eastern Visayas"
        ],
        ["mindanao"] =
        [
            "Zamboanga Peninsula", "Northern Mindanao", "Davao Region", "SOCCSKSARGEN",
            "Caraga", "BARMM"
        ]
    };
    public async Task<PaginatedResponse<ServiceProviderDto>> Handle(GetAllServiceProvidersRequest request, CancellationToken cancellationToken)
    {
        var allServiceProvidersQuery = _serviceProviderRepository.Query();

        // Early performance optimization: Apply non-spatial filters first to reduce dataset
        if (request.ProviderIds?.Count > 0)
        {
            allServiceProvidersQuery = allServiceProvidersQuery.Where(s => request.ProviderIds.Contains(s.ProviderId));
        }
        
        if (request.ServiceIds?.Count > 0)
        {
            allServiceProvidersQuery = allServiceProvidersQuery.Where(s => request.ServiceIds.Contains(s.ServiceId));
        }

        // Optimize service type filtering
        if (request.ServiceTypes?.Count > 0)
        {
            var requestedServiceTypes = request.ServiceTypes
                .Select(st => EnumHelper<ServiceTypeEnum>.GetValue(st))
                .ToHashSet();
            
            allServiceProvidersQuery = allServiceProvidersQuery
                .Where(s => requestedServiceTypes.Contains(s.Service.Type));
        }

        // Optimized geographic filtering
        if ((request.Areas?.Count > 0) || (request.Regions?.Count > 0))
        {
            var targetRegions = GetTargetRegions(request.Areas, request.Regions);
            
            if (targetRegions.Count > 0)
            {
                // Single database call to get all region boundaries with union geometry
                var unionedBoundary = await GetUnionedRegionBoundariesAsync(targetRegions, cancellationToken);
                
                if (unionedBoundary != null)
                {
                    // More efficient spatial filtering using a single union geometry
                    allServiceProvidersQuery = allServiceProvidersQuery
                        .Where(sp => 
                            // For Wireless services - check if any cells are within target regions
                            (sp.Service.Type == ServiceTypeEnum.Wireless &&
                             sp.Cells.Any(c => c.Location != null && unionedBoundary.Intersects(c.Location))) ||
                            // For Wired services - check if any NAPs are within target regions  
                            (sp.Service.Type == ServiceTypeEnum.Wired &&
                             sp.NAPs.Any(n => n.Location != null && unionedBoundary.Intersects(n.Location)))
                        );
                }
            }
        }

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

        return await _serviceProviderRepository.PaginateAsync(serviceProviderDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
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
                var areaKey = area.Trim().ToLower();
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

    /// <summary>
    /// Creates a single union geometry from multiple region boundaries for efficient spatial queries
    /// </summary>
    private async Task<Geometry?> GetUnionedRegionBoundariesAsync(HashSet<string> targetRegions, CancellationToken cancellationToken)
    {
        // Single query to get all region boundaries
        var regionBoundaries = await _regionRepository
            .Query(r => targetRegions.Contains(r.Name) && r.Boundaries != null)
            .Select(r => r.Boundaries!)
            .ToListAsync(cancellationToken);

        if (regionBoundaries.Count == 0)
            return null;

        // For single region, return as-is
        if (regionBoundaries.Count == 1)
            return regionBoundaries[0];

        // Union all boundaries into a single geometry for optimal spatial querying
        try
        {
            Geometry union = regionBoundaries[0];
            for (int i = 1; i < regionBoundaries.Count; i++)
            {
                union = union.Union(regionBoundaries[i]);
            }
            return union;
        }
        catch
        {
            // Fallback: if union fails, use the largest boundary
            return regionBoundaries.OrderByDescending(b => b.Area).First();
        }
    }
}
