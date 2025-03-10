using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Requests;

public class CreateNetworkTemplateRequest(CreateNetworkTemplateDto createNetworkTemplate) : IRequest<ApiResponse<NetworkTemplateDto>>
{
    public CreateNetworkTemplateDto CreateNetworkTemplate { get; set; } = createNetworkTemplate;
}
