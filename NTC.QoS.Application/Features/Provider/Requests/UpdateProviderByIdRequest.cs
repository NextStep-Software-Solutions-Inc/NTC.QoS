using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class UpdateProviderByIdRequest(Guid id, UpdateProviderDto provider) : IRequest<ApiResponse<ProviderDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateProviderDto Provider { get; set; } = provider;
}
