using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Queries;

public class GetAllServiceProvidersHandler(IRepository<Domain.Aggregates.ServiceProvider> repository) : IRequestHandler<GetAllServiceProvidersRequest, PaginatedResponse<ServiceProviderDto>>
{
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _repository = repository;
    public async Task<PaginatedResponse<ServiceProviderDto>> Handle(GetAllServiceProvidersRequest request, CancellationToken cancellationToken)
    {
        var allServiceProviders = await _repository.ListAsync(s => s.IsDeleted != true, cancellationToken);
        var serviceProviders = allServiceProviders.Select(serviceProvider => new ServiceProviderDto
        {
            Id = serviceProvider.Id,
            ProviderId = serviceProvider.ProviderId,
            ServiceId = serviceProvider.ServiceId,
            MCC = serviceProvider.MCC,
            MNC = serviceProvider.MNC
        }).ToList();
        return new PaginatedResponse<ServiceProviderDto>(serviceProviders, request.PageNumber, request.PageSize);
    }
}