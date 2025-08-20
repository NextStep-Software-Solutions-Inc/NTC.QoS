using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class GetProviderByIdRequest(Guid id) : IRequest<ApiResponse<ProviderDto>>
{
    public Guid Id { get; set; } = id;
}
