using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Requests;

public class GetServiceProviderByIdRequest : IRequest<ApiResponse<ServiceProviderDto>>
{
    public GetServiceProviderByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
