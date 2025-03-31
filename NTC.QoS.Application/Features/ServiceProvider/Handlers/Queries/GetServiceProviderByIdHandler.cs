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

public class GetServiceProviderByIdHandler(IServiceProviderRepository serviceProviderRepository) : IRequestHandler<GetServiceProviderByIdRequest, ApiResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    public async Task<ApiResponse<ServiceProviderDto>> Handle(GetServiceProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var serviceProvider = await _serviceProviderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<ServiceProviderDto>
            {
                Succeeded = false,
                Message = "Service provider does not exists.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullServiceProvider", ["Service provider not found."]
                    }
                }
            };
        }

        return new ApiResponse<ServiceProviderDto>
        {
            Data = new ServiceProviderDto
            {
                Id = serviceProvider.Id,
                Provider = new ProviderDto
                {
                    Id = serviceProvider.ProviderId,
                    Name = serviceProvider.Provider?.Name ?? string.Empty,
                    Description = serviceProvider.Provider?.Description ?? string.Empty,
                    LicenseNumber = serviceProvider.Provider?.LicenseNumber ?? string.Empty,
                    Color = serviceProvider.Provider?.Color ?? string.Empty
                },
                Service = new ServiceDto
                {
                    Id = serviceProvider.ServiceId,
                    Name = serviceProvider.Service?.Name ?? string.Empty,
                    Type = serviceProvider.Service?.Type.ToString() ?? string.Empty
                },
                MNC = serviceProvider.MNC,
                MCC = serviceProvider.MCC
            },
            Succeeded = true,
            Message = "Service provider retrieved successfully."
        };
    }
}
