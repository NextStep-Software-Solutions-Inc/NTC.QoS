using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Requests;

public class GetServiceProviderByIdRequest(Guid id) : IRequest<ApiResponse<ServiceProviderDto>>
{
    public Guid Id { get; set; } = id;
}
