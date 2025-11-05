using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetServiceProviderByIdHandler(IServiceProviderRepository serviceProviderRepository) : IRequestHandler<GetServiceProviderByIdRequest, ApiResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    
    public async Task<ApiResponse<ServiceProviderDto>> Handle(GetServiceProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var serviceProviderDtoQuery = _serviceProviderRepository.Query(sp => sp.Id.Equals(request.Id))
            .Select(serviceProvider => new ServiceProviderDto
            {
                Id = serviceProvider.Id,
                Provider = serviceProvider.Provider != null ? new()
                {
                    Id = serviceProvider.ProviderId,
                    Name = serviceProvider.Provider.Name,
                    Description = serviceProvider.Provider.Description,
                    LicenseNumber = serviceProvider.Provider.LicenseNumber,
                    Color = serviceProvider.Provider.Color
                } : null,
                Service = serviceProvider.Service != null ? new()
                {
                    Id = serviceProvider.ServiceId,
                    Name = serviceProvider.Service.Name,
                    Type = serviceProvider.Service.Type.ToString()
                } : null,
                MNC = serviceProvider.MNC,
                MCC = serviceProvider.MCC
            });

        var serviceProviderDto = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderDtoQuery, cancellationToken);

        if (serviceProviderDto is null)
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
            Data = serviceProviderDto,
            Succeeded = true,
            Message = "Service provider retrieved successfully."
        };
    }
}
