using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;
namespace NTC.QoS.Application.Features.Provider.Requests;

public class GetProviderByIdRequest : IRequest<ApiResponse<ProviderDto>>
{
    public GetProviderByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


