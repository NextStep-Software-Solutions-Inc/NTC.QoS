using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Requests;

public class CreateServiceProviderRequest : IRequest<ApiResponse<Domain.Aggregates.ServiceProvider>>
{
    public CreateServiceProviderRequest(CreateServiceProviderDto serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
    public CreateServiceProviderDto ServiceProvider { get; set; }
}