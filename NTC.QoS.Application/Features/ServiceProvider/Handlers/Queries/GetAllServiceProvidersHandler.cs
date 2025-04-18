using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetAllServiceProvidersHandler (IServiceProviderRepository serviceProviderRepository) : IRequestHandler<GetAllServiceProvidersRequest, PaginatedResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    public async Task<PaginatedResponse<ServiceProviderDto>> Handle(GetAllServiceProvidersRequest request, CancellationToken cancellationToken)
    {
        var allServiceProvidersQuery = _serviceProviderRepository.Query(s => !s.IsDeleted);

        allServiceProvidersQuery = allServiceProvidersQuery
            .Where(s => !request.ProviderId.HasValue || s.ProviderId == request.ProviderId.Value)
            .Where(s => !request.ServiceId.HasValue || s.ServiceId == request.ServiceId.Value);

        var serviceProviderDtosQuery = allServiceProvidersQuery
            .OrderBy(s => s.CreatedAt)
            .Select(serviceProvider => new ServiceProviderDto
            {
                Id = serviceProvider.Id,
                Provider = new ()
                {
                    Id = serviceProvider.ProviderId,
                    Name = serviceProvider.Provider.Name,
                    Description = serviceProvider.Provider.Description,
                    LicenseNumber = serviceProvider.Provider.LicenseNumber,
                    Color = serviceProvider.Provider.Color
                },
                Service = new ()
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
