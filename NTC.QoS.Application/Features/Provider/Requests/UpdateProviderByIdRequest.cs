using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class UpdateProviderByIdRequest : IRequest<ApiResponse<ProviderDto>>
{
    public Guid Id { get; set; }
    public UpdateProviderDto Provider { get; set; } = null!;

    public UpdateProviderByIdRequest(Guid id, UpdateProviderDto provider)
    {
        Id = id;
        Provider = provider;
    }
}
