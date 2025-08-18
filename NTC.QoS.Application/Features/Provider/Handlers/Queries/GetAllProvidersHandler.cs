using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;

public class GetAllProvidersHandler(IProviderRepository providerRepository, IMunicipalityRepository municipalityRepository) : IRequestHandler<GetAllProvidersRequest, PaginatedResponse<ProviderDto>>
{
    private readonly IProviderRepository _providerRepository = providerRepository;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;

    // Define the regions that belong to each major area
    private static readonly Dictionary<string, string[]> AreaRegions = new()
    {
        ["luzon"] = new[]
        {
            "Ilocos Region", "Cagayan Valley", "Central Luzon", "CALABARZON", "MIMAROPA",
            "Bicol Region", "National Capital Region", "Cordillera Administrative Region"
        },
        ["visayas"] = new[]
        {
            "Western Visayas", "Central Visayas", "Eastern Visayas"
        },
        ["mindanao"] = new[]
        {
            "Zamboanga Peninsula", "Northern Mindanao", "Davao Region", "SOCCSKSARGEN",
            "Caraga", "BARMM"
        }
    };

    public async Task<PaginatedResponse<ProviderDto>> Handle(GetAllProvidersRequest request, CancellationToken cancellationToken)
    {
        var allProvidersQuery = _providerRepository.Query();
        
        // Apply service filtering
        allProvidersQuery = allProvidersQuery
            .Where(p => request.ServiceIds == null || request.ServiceIds.Count.Equals(0) || p.ServiceProviders.Any(sp => sp.Service != null && request.ServiceIds.Contains(sp.Service.Id)));

        // Apply name filtering
        allProvidersQuery = allProvidersQuery
            .Where(p => request.Names == null || request.Names.Any(n => p.Name.Trim().ToLower().Contains(n.Trim().ToLower())));

        // Apply area filtering using the geographic hierarchy
        if (request.Areas != null && request.Areas.Count > 0)
        {
            // Get all region names that belong to the requested areas
            var targetRegions = new List<string>();
            foreach (var area in request.Areas)
            {
                var areaKey = area.Trim().ToLower();
                if (AreaRegions.TryGetValue(areaKey, out var regions))
                {
                    targetRegions.AddRange(regions);
                }
            }

            if (targetRegions.Count > 0)
            {
                // First, get all municipality IDs that belong to the target regions
                var targetMunicipalityIds = await _municipalityRepository
                    .Query(m => targetRegions.Contains(m.Province.Region.Name))
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                if (targetMunicipalityIds.Count > 0)
                {
                    // Filter providers that have cells within the target municipalities using spatial intersection
                    allProvidersQuery = allProvidersQuery
                        .Where(p => p.ServiceProviders.Any(sp => 
                            sp.Cells.Any(c => 
                                c.Location != null && 
                                targetMunicipalityIds.Any(municipalityId => 
                                    _municipalityRepository.Query(m => m.Id == municipalityId)
                                        .Any(m => m.Boundaries != null && m.Boundaries.Intersects(c.Location))))));
                }
            }
        }

        var providerDtosQuery = allProvidersQuery
            .OrderBy(p => p.CreatedAt)
            .Select(p => new ProviderDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                LicenseNumber = p.LicenseNumber,
                Color = p.Color
            });

        return await _providerRepository.PaginateAsync(providerDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
