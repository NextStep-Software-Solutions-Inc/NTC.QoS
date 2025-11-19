using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetAllServiceProvidersHandler(IServiceProviderRepository serviceProviderRepository) : IRequestHandler<GetAllServiceProvidersRequest, PaginatedResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
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
