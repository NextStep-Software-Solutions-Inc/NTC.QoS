using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Requests;

public class GetNetworkTemplateByIdRequest : IRequest<ApiResponse<NetworkTemplateDto>>
{
    public GetNetworkTemplateByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
