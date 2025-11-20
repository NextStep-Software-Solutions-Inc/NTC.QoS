using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetAllServiceProvidersHandler(
    IServiceProviderRepository serviceProviderRepository,
    IRepository<Domain.Entities.Region> regionRepository,
    ICellRepository cellRepository,
    INAPRepository napRepository) : IRequestHandler<GetAllServiceProvidersRequest, PaginatedResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly INAPRepository _napRepository = napRepository;

    public async Task<PaginatedResponse<ServiceProviderDto>> Handle(GetAllServiceProvidersRequest request, CancellationToken cancellationToken)
    {
        var allServiceProvidersQuery = _serviceProviderRepository.QueryAsNoTracking();

        var requestedServiceTypes = request.ServiceTypes?
            .Select(st => EnumHelper<ServiceTypeEnum>.GetValue(st))
            .ToList();

        allServiceProvidersQuery = allServiceProvidersQuery
            .Where(s => request.ProviderIds == null || request.ProviderIds.Contains(s.ProviderId))
            .Where(s => request.ServiceIds == null || request.ServiceIds.Contains(s.ServiceId))
            .Where(s => request.ServiceTypes == null || (requestedServiceTypes != null && requestedServiceTypes.Contains(s.Service.Type)));

        // Apply geographic filters (Areas and Regions)
        if (request.Areas?.Count > 0 || request.Regions?.Count > 0)
        {
            var targetRegions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add regions from Areas mapping
            if (request.Areas?.Count > 0)
            {
                foreach (var area in request.Areas)
                {
                    var areaKey = area.Trim().ToLowerInvariant();
                    if (GeoSpatialConstants.AreaRegions.TryGetValue(areaKey, out var mappedRegions))
                    {
                        foreach (var region in mappedRegions)
                        {
                            targetRegions.Add(region);
                        }
                    }
                }
            }

            // Add direct regions
            if (request.Regions?.Count > 0)
            {
                foreach (var region in request.Regions)
                {
                    targetRegions.Add(region.Trim());
                }
            }

            if (targetRegions.Count > 0)
            {
                // Get region envelopes (Bounding Boxes)
                var regionEnvelopes = await _regionRepository
                    .Query(r => targetRegions.Contains(r.Name) && r.Boundaries != null)
                    .Select(r => r.Boundaries!.Envelope)
                    .ToListAsync(cancellationToken);

                if (regionEnvelopes.Count > 0)
                {
                    // Find service provider IDs that have cells in the target region envelopes
                    var cellQuery = _cellRepository.Query();
                    cellQuery = GeoQueryHelper.ApplyMultipleEnvelopeFilter(regionEnvelopes, cellQuery);
                    
                    var wirelessServiceProviderIds = await cellQuery
                        .Select(c => c.ServiceProviderId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    // Find service provider IDs that have NAPs in the target region envelopes
                    var napQuery = _napRepository.Query();
                    napQuery = GeoQueryHelper.ApplyMultipleEnvelopeFilter(regionEnvelopes, napQuery);

                    var wiredServiceProviderIds = await napQuery
                        .Select(n => n.ServiceProviderId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    // Combine and filter by service type
                    var allMatchingIds = wirelessServiceProviderIds.Concat(wiredServiceProviderIds).Distinct().ToList();

                    if (allMatchingIds.Count > 0)
                    {
                        allServiceProvidersQuery = allServiceProvidersQuery.Where(sp => 
                            allMatchingIds.Contains(sp.Id) &&
                            ((sp.Service.Type == ServiceTypeEnum.Wireless && wirelessServiceProviderIds.Contains(sp.Id)) ||
                             (sp.Service.Type == ServiceTypeEnum.Wired && wiredServiceProviderIds.Contains(sp.Id)))
                        );
                    }
                    else
                    {
                        // No matching providers found
                        allServiceProvidersQuery = allServiceProvidersQuery.Where(sp => false);
                    }
                }
                else
                {
                    // No regions found
                    allServiceProvidersQuery = allServiceProvidersQuery.Where(sp => false);
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
}
