using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Requests;

public class UpdateServiceProviderByIdRequest : IRequest<ApiResponse<Domain.Aggregates.ServiceProvider>>
{
    public UpdateServiceProviderByIdRequest(Guid id, UpdateServiceProviderDto serviceProvider)
    {
        Id = id;
        ServiceProvider = serviceProvider;
    }
    public Guid Id { get; set; }
    public UpdateServiceProviderDto ServiceProvider { get; set; }
}
