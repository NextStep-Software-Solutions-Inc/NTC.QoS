using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Requests;

public class CreateNetworkTemplateRequest : IRequest<ApiResponse<NetworkTemplateDto>>
{
    public CreateNetworkTemplateRequest(CreateNetworkTemplateDto createNetworkTemplate)
    {
        CreateNetworkTemplate = createNetworkTemplate;
    }
    public CreateNetworkTemplateDto CreateNetworkTemplate { get; set; }
}
