using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetAllServiceProvidersHandler (IServiceProviderRepository serviceProviderRepository) : IRequestHandler<GetAllServiceProvidersRequest, PaginatedResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    public async Task<PaginatedResponse<ServiceProviderDto>> Handle(GetAllServiceProvidersRequest request, CancellationToken cancellationToken)
    {
        var allServiceProviders = await _serviceProviderRepository.ListAsync(s => s.IsDeleted != true, cancellationToken);
        var serviceProviders = allServiceProviders.Select(static serviceProvider => new ServiceProviderDto
        {
            Id = serviceProvider.Id,
            ProviderId = serviceProvider.ProviderId,
            Provider = new() 
            {
                Name = serviceProvider.Provider!.Name,
                Description = serviceProvider.Provider.Description,
                LicenseNumber = serviceProvider.Provider.LicenseNumber,
            },
            ServiceId = serviceProvider.ServiceId,
            Service = new() 
            { 
                Name = serviceProvider.Service!.Name,
                Type = EnumHelper<ServiceTypeEnum>.GetName(serviceProvider.Service.Type)
            },
            MCC = serviceProvider.MCC,
            MNC = serviceProvider.MNC
        }).ToList();
        return new PaginatedResponse<ServiceProviderDto>(serviceProviders, request.PageNumber, request.PageSize);
    }
}