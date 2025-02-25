using MediatR;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class CreateProviderRequest : IRequest<ApiResponse<CreateProviderDto>>
{
    public CreateProviderDto Provider{ get; set; } = new();
}
