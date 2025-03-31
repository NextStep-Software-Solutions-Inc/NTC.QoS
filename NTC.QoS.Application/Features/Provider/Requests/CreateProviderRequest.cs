using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class CreateProviderRequest(CreateProviderDto createProvider) : IRequest<ApiResponse<ProviderDto>>
{
    public CreateProviderDto Provider { get; set; } = createProvider;
}
