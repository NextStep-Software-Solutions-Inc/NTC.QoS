using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Application.Services.Interface;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;
using System.Diagnostics;

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

        var stopwatch = Stopwatch.StartNew();
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

            var filterStopwatch = Stopwatch.StartNew();

            // Apply geographic filtering
            if ((request.Areas?.Count > 0) || (request.Regions?.Count > 0))
            {
                allServiceProvidersQuery = await _geographicFilteringService.ApplyGeographicFiltersAsync(
                    allServiceProvidersQuery, request.Areas, request.Regions, timeoutCts.Token);
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
}