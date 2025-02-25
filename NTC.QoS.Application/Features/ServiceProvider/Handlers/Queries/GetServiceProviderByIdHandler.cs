using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetServiceProviderByIdHandler(IServiceProviderRepository serviceProviderRepository) : IRequestHandler<GetServiceProviderByIdRequest, ApiResponse<ServiceProviderDto>>
{
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    public async Task<ApiResponse<ServiceProviderDto>> Handle(GetServiceProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var serviceProvider = await _serviceProviderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (serviceProvider == null)
        {
            return new ApiResponse<ServiceProviderDto>(
            data: null!,
            succeeded: false,
            message: "Service Provider not found");
        }
        var serviceProviderDto = new ServiceProviderDto
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
        };
        return new ApiResponse<ServiceProviderDto>(serviceProviderDto, true, "Service Provider found");
    }
}
