using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetAllServiceProvidersHandler (IServiceProviderRepository serviceProviderRepository) : IRequestHandler<GetAllServiceProvidersRequest, PaginatedResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    public async Task<PaginatedResponse<ServiceProviderDto>> Handle(GetAllServiceProvidersRequest request, CancellationToken cancellationToken)
    {
        var allServiceProviders = await _serviceProviderRepository.ListAsync(s => !s.IsDeleted, cancellationToken);

        allServiceProviders = [.. allServiceProviders
            .Where(s => !request.ProviderId.HasValue || s.ProviderId == request.ProviderId.Value)
            .Where(s => !request.ServiceId.HasValue || s.ServiceId == request.ServiceId.Value)];

        var serviceProviderDtos = allServiceProviders.Select(serviceProvider => new ServiceProviderDto
        {
            Id = serviceProvider.Id,
            Provider = new ProviderDto
            {
                Id = serviceProvider.ProviderId,
                Name = serviceProvider.Provider?.Name ?? string.Empty,
                Description = serviceProvider.Provider?.Description ?? string.Empty,
                LicenseNumber = serviceProvider.Provider?.LicenseNumber ?? string.Empty
            },
            Service = new ServiceDto
            {
                Id = serviceProvider.ServiceId,
                Name = serviceProvider.Service?.Name ?? string.Empty,
                Type = serviceProvider.Service?.Type.ToString() ?? string.Empty
            },
            MNC = serviceProvider.MNC,
            MCC = serviceProvider.MCC
        }).ToList();

        if (serviceProviderDtos.Count is 0)
        {
            return new PaginatedResponse<ServiceProviderDto>(serviceProviderDtos, request.PageNumber, request.PageSize)
            {
                Data = serviceProviderDtos,
                Message = "No records found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullServiceProviders", ["No service providers found."]
                    }
                }
            };
        }

        return new PaginatedResponse<ServiceProviderDto>(serviceProviderDtos, request.PageNumber, request.PageSize)
        {
            Data = serviceProviderDtos,
            Succeeded = true,
            Message = "Service Providers retrieved successfully."
        };
    }
}
