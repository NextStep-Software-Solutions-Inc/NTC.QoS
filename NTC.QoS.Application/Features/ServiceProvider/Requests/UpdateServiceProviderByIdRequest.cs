using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Requests;

public class UpdateServiceProviderByIdRequest(Guid id, UpdateServiceProviderDto serviceProvider) : IRequest<ApiResponse<ServiceProviderDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateServiceProviderDto ServiceProvider { get; set; } = serviceProvider;
}
