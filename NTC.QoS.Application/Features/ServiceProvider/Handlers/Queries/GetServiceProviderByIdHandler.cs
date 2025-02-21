using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetServiceProviderByIdHandler(IRepository<Domain.Aggregates.ServiceProvider> repository) : IRequestHandler<GetServiceProviderByIdRequest, ApiResponse<ServiceProviderDto>>
{
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _repository = repository;
    public async Task<ApiResponse<ServiceProviderDto>> Handle(GetServiceProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var serviceProvider = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (serviceProvider == null)
        {
            return new ApiResponse<ServiceProviderDto>(data: new ServiceProviderDto(), succeeded: false, message: "Service Provider not found");
        }
        var serviceProviderDto = new ServiceProviderDto
        {
            Id = serviceProvider.Id,
            ProviderId = serviceProvider.ProviderId,
            ServiceId = serviceProvider.ServiceId,
            MCC = serviceProvider.MCC,
            MNC = serviceProvider.MNC
        };
        return new ApiResponse<ServiceProviderDto>(serviceProviderDto, true, "Service Provider found");
    }
}
