using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Application.Services.Interface;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetAllServiceProvidersHandler : IRequestHandler<GetAllServiceProvidersRequest, PaginatedResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository;
    private readonly IGeographicFilteringService _geographicFilteringService;
    private readonly ILogger<GetAllServiceProvidersHandler> _logger;

    public GetAllServiceProvidersHandler(
        IServiceProviderRepository serviceProviderRepository,
        IGeographicFilteringService geographicFilteringService,
        ILogger<GetAllServiceProvidersHandler> logger)
    {
        _serviceProviderRepository = serviceProviderRepository;
        _geographicFilteringService = geographicFilteringService;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ServiceProviderDto>> Handle(GetAllServiceProvidersRequest request, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(3));

        _logger.LogInformation("Starting GetAllServiceProviders query with filters: Areas={Areas}, Regions={Regions}",
            string.Join(",", request.Areas ?? []), string.Join(",", request.Regions ?? []));

        try
        {
            var allServiceProvidersQuery = _serviceProviderRepository.Query();

            if (request.ProviderIds?.Count > 0)
            {
                allServiceProvidersQuery = allServiceProvidersQuery.Where(s => request.ProviderIds.Contains(s.ProviderId));
            }

            if (request.ServiceIds?.Count > 0)
            {
                allServiceProvidersQuery = allServiceProvidersQuery.Where(s => request.ServiceIds.Contains(s.ServiceId));
            }

            if (request.ServiceTypes?.Count > 0)
            {
                var requestedServiceTypes = request.ServiceTypes
                    .Select(st => EnumHelper<ServiceTypeEnum>.GetValue(st))
                    .ToHashSet();
                allServiceProvidersQuery = allServiceProvidersQuery.Where(s => requestedServiceTypes.Contains(s.Service.Type));
            }

            // Apply geographic filtering
            if ((request.Areas?.Count > 0) || (request.Regions?.Count > 0))
            {
                allServiceProvidersQuery = await _geographicFilteringService.ApplyGeographicFiltersAsync(
                    allServiceProvidersQuery, request.Areas, request.Regions, timeoutCts.Token);
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

            var result = await _serviceProviderRepository.PaginateAsync(serviceProviderDtosQuery, request.PageNumber, request.PageSize, timeoutCts.Token);

            _logger.LogInformation("Query completed successfully, returned {Count} items", result.Data?.Count ?? 0);

            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Query timed out");
            throw new TimeoutException("Query timed out. Please try with more specific filters.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution failed");
            throw;
        }
    }
}