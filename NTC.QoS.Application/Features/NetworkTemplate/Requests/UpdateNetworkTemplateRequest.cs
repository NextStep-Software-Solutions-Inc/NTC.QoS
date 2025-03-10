using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Requests;

public class UpdateNetworkTemplateRequest(Guid id, UpdateNetworkTemplateDto networkTemplate) : IRequest<ApiResponse<NetworkTemplateDto>>
{
    public UpdateNetworkTemplateDto NetworkTemplate { get; set; } = networkTemplate;
    public Guid Id { get; set; } = id;
}
